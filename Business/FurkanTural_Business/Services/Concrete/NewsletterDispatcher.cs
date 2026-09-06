using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Dağıtıma verilmiş bülten sayılarını gönderir. Bir tur, gördüğü her bekleyen satırı <b>bir kez</b> dener ve aynı satıra o tur içinde geri dönmez; ilerleme kimlik imleciyle yürür. Bunun sonucu bir geri çekilme davranışıdır: geçici bir SMTP arızasında deneme hakları saniyeler içinde tükenmez, denemeler turlar arası beklemeyle kendiliğinden aralanır ve ayrı bir bekleme sütunu gerekmez.<para>Okumaların tamamı küresel süzgeçten geçer. Bu bir tercih değil tutarlılık şartıdır: dağıtıcının gördüğü satır ile sayaçların saydığı satır aynı süzgeçten geçmezse, bitmiş görünen bir dağıtım hiç bitmez ya da bitmemiş olan bitmiş sayılırdı. Aynı süzgeç sayıyı pasife almayı ücretsiz bir duraklatmaya çevirir.</para><para>Her alıcıya kendi çıkış jetonu üretilir ve <b>yalnızca gönderim başarılıysa</b> kaydedilir; gönderilemeyen bir postanın jetonu veri tabanında iz bırakmaz. Ters sıra — önce kaydet sonra gönder — başarısız her denemede ölü bir kimlik bilgisi biriktirirdi.</para><para>Gönderim anında abonelik durumu yeniden okunur. Liste dondurulduktan sonra çıkanlar atlanır: dondurma anını değil gönderim anını esas almak, çıkışa saygının tek doğru yorumudur.</para></summary>
public class NewsletterDispatcher(
    IUnitOfWork unitOfWork,
    IMailSender mailSender,
    IConfiguration configuration,
    ILogger<NewsletterDispatcher> logger,
    IClock clock) : INewsletterDispatcher
{
    /// <summary>Bir kaydetmede işlenen dağıtım satırı sayısı. Küçük tutulur: tur ortasında düşen bir süreçte yalnızca bu kadar satırın sonucu yazılmamış kalır ve o satırlar bekleyen olarak yeniden denenir.</summary>
    public const int BatchSize = 25;

    /// <summary>Bir alıcı için toplam deneme hakkı. Tükendiğinde satır kalıcı olarak başarısız sayılır ve son hata metni yerinde kalır.</summary>
    public const int MaxAttempts = 3;

    /// <summary>Tek turda ele alınan sayı adedi. Aynı anda birden çok bültenin dağıtımda olması beklenmez; sınır yalnızca turun sınırsız uzamasını engeller.</summary>
    public const int IssuesPerPass = 5;

    /// <summary>Bültene gömülen çıkış bağlantısının ömrü. İstek üzerine üretilen yirmi dört saatlik bağlantıdan bilerek çok uzundur: bülten gelen kutusunda aylarca durabilir ve çıkış bağlantısının o gün çalışmaması, listenin izinli kalmasını kullanıcının hafızasına bağlamak olurdu.</summary>
    public static readonly TimeSpan UnsubscribeLifetime = TimeSpan.FromDays(1095);

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMailSender _mailSender = mailSender;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<NewsletterDispatcher> _logger = logger;
    private readonly IClock _clock = clock;

    public async Task<int> DispatchAsync(CancellationToken cancellationToken = default)
    {
        var unsubscribeUrl = _configuration["Newsletter:UnsubscribeUrl"];
        if (string.IsNullOrWhiteSpace(unsubscribeUrl))
        {
            _logger.LogWarning("Bülten dağıtımı yapılamadı: Newsletter:UnsubscribeUrl yapılandırılmamış.");
            return 0;
        }

        var issues = await _unitOfWork.NewsletterIssues.GetAllPagedAsync(
            1, IssuesPerPass, x => x.Status == NewsletterIssueStatuses.Sending, false, cancellationToken);

        var processed = 0;
        foreach (var issue in issues)
        {
            if (cancellationToken.IsCancellationRequested) break;
            processed += await RunIssueAsync(issue, unsubscribeUrl!, cancellationToken);
        }

        return processed;
    }

    private async Task<int> RunIssueAsync(NewsletterIssue issue, string unsubscribeUrl, CancellationToken cancellationToken)
    {
        var processed = 0;
        var cursor = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = (await _unitOfWork.NewsletterDeliveries.GetAllPagedAsync(
                1, BatchSize,
                d => d.NewsletterIssueId == issue.Id
                     && d.Status == NewsletterDeliveryStatuses.Pending
                     && d.AttemptCount < MaxAttempts
                     && d.Id > cursor,
                false, cancellationToken)).ToList();

            if (batch.Count == 0) break;

            var live = await LiveSubscriberIdsAsync(batch, cancellationToken);

            foreach (var delivery in batch)
            {
                cursor = delivery.Id;
                await AttemptAsync(issue, delivery, live.Contains(delivery.SubscriberId), unsubscribeUrl, cancellationToken);
                processed++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await RefreshAsync(issue, cancellationToken);
        return processed;
    }

    private async Task AttemptAsync(NewsletterIssue issue, NewsletterDelivery delivery, bool subscribed, string unsubscribeUrl, CancellationToken cancellationToken)
    {
        if (!subscribed)
        {
            delivery.Status = NewsletterDeliveryStatuses.Skipped;
            delivery.Error = "Liste dondurulduktan sonra abonelik sona erdi.";
            await _unitOfWork.NewsletterDeliveries.UpdateAsync(delivery, cancellationToken);
            return;
        }

        var token = GenerateToken();
        var sent = await _mailSender.SendAsync(
            MailTemplateDefinitions.NewsletterIssue, AppSourceDefinitions.Blog, delivery.Email,
            new NewsletterIssueMailDto
            {
                Subject = issue.Subject,
                Body = issue.Body,
                Email = delivery.Email,
                UnsubscribeUrl = $"{unsubscribeUrl}?token={Uri.EscapeDataString(token)}",
                ContactEmail = _configuration["Contact:ContactEmail"] ?? "",
                CurrentYear = _clock.UtcNow.Year.ToString()
            },
            cancellationToken);

        delivery.AttemptCount++;

        if (sent.Success)
        {
            await _unitOfWork.SubscriberVerifications.AddAsync(new SubscriberVerification
            {
                SubscriberId = delivery.SubscriberId,
                TokenHash = Hash(token),
                Purpose = SubscriberVerificationPurposes.Unsubscribe,
                ExpiresAt = _clock.UtcNow.Add(UnsubscribeLifetime)
            }, cancellationToken);

            delivery.Status = NewsletterDeliveryStatuses.Sent;
            delivery.SentAt = _clock.UtcNow;
            delivery.Error = null;
        }
        else
        {
            delivery.Error = Truncate(sent.InternalMessage ?? sent.Message, 500);

            if (delivery.AttemptCount >= MaxAttempts)
            {
                delivery.Status = NewsletterDeliveryStatuses.Failed;
                _logger.LogWarning("Bülten gönderimi kalıcı olarak başarısız. Sayı: {IssueId}, Dağıtım: {DeliveryId}", issue.Id, delivery.Id);
            }
        }

        await _unitOfWork.NewsletterDeliveries.UpdateAsync(delivery, cancellationToken);
    }

    /// <summary>Kümedeki abonelerden gönderim anında hâlâ doğrulanmış ve listede olanların kimlikleri. Tek sorguyla okunur; alıcı başına ayrı sorgu, SMTP turunun yanına gereksiz bir gidiş dönüş daha eklerdi.</summary>
    private async Task<HashSet<int>> LiveSubscriberIdsAsync(List<NewsletterDelivery> batch, CancellationToken cancellationToken)
    {
        var ids = batch.Select(d => d.SubscriberId).Distinct().ToList();
        var live = await _unitOfWork.Subscribers.GetAllAsync(s => ids.Contains(s.Id) && s.ConfirmedAt != null, cancellationToken);
        return [.. live.Select(s => s.Id)];
    }

    /// <summary>Sayaçları dağıtım satırlarından yeniden sayar ve bekleyen kalmadıysa sayıyı kapatır. Elde tutulan bir toplamı artırmak yerine yeniden saymak, tur ortasında düşen bir süreçten sonra rakamların kaymamasını garanti eder.</summary>
    private async Task RefreshAsync(NewsletterIssue issue, CancellationToken cancellationToken)
    {
        var sent = await CountAsync(issue.Id, NewsletterDeliveryStatuses.Sent, cancellationToken);
        var failed = await CountAsync(issue.Id, NewsletterDeliveryStatuses.Failed, cancellationToken);
        var skipped = await CountAsync(issue.Id, NewsletterDeliveryStatuses.Skipped, cancellationToken);
        var pending = await CountAsync(issue.Id, NewsletterDeliveryStatuses.Pending, cancellationToken);

        var completed = pending == 0;
        if (issue.SentCount == sent && issue.FailedCount == failed && issue.SkippedCount == skipped && !completed)
            return;

        issue.SentCount = sent;
        issue.FailedCount = failed;
        issue.SkippedCount = skipped;

        if (completed)
        {
            issue.Status = NewsletterIssueStatuses.Sent;
            issue.CompletedAt = _clock.UtcNow;
        }

        await _unitOfWork.NewsletterIssues.UpdateAsync(issue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (completed)
            _logger.LogInformation(
                "Bülten dağıtımı bitti. Sayı: {IssueId}, Gönderilen: {Sent}, Başarısız: {Failed}, Atlanan: {Skipped}",
                issue.Id, sent, failed, skipped);
    }

    private Task<int> CountAsync(int issueId, string status, CancellationToken cancellationToken)
        => _unitOfWork.NewsletterDeliveries.CountAsync(d => d.NewsletterIssueId == issueId && d.Status == status, cancellationToken);

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Hash(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
