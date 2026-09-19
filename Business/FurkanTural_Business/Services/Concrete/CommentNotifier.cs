using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Bekleyen yanıt bildirimlerini gönderir; kalıbı <see cref="NewsletterDispatcher"/> ile aynıdır ve aynı gerekçelerle öyledir — bir tur her satırı bir kez dener, kimlik imleciyle ilerler ve aynı satıra o tur içinde dönmez, dolayısıyla deneme hakları geçici bir SMTP arızasında saniyeler içinde tükenmez.<para>Bültenden bir yerde ayrılır: alıcı listesi dondurulmuş değildir, tek bir kişidir ve o kişinin durumu gönderim anında yeniden okunur. Kuyruğa girdikten sonra bildirimleri kapatmış, yanıtı reddedilmiş ya da yazısı yayından kalkmış olabilir; bu satırlar başarısız değil <b>atlanmış</b> sayılır.</para><para>Çıkış jetonunun süresi yoktur. Bülten jetonu üç yılla sınırlıdır çünkü liste yenilenir; yorum bildirimi ise gelen kutusunda yıllarca durabilir ve o gün çalışmayan bir kapatma bağlantısı, adresin izinli kalmasını kullanıcının sabrına bağlamak olurdu. Jeton tek bir adresin bildirimlerini kapatmaktan başka hiçbir şey yapamaz.</para></summary>
public class CommentNotifier(
    IUnitOfWork unitOfWork,
    IMailSender mailSender,
    IConfiguration configuration,
    ActivityLogger activityLogger,
    ILogger<CommentNotifier> logger,
    IClock clock) : ICommentNotifier
{
    /// <summary>Bir kaydetmede işlenen bildirim sayısı. Küçük tutulur: tur ortasında düşen bir süreçte yalnızca bu kadar satırın sonucu yazılmamış kalır ve o satırlar bekleyen olarak yeniden denenir.</summary>
    public const int BatchSize = 25;

    /// <summary>Bir bildirim için toplam deneme hakkı. Tükendiğinde satır kalıcı olarak başarısız sayılır ve son hata metni yerinde kalır.</summary>
    public const int MaxAttempts = 3;

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMailSender _mailSender = mailSender;
    private readonly IConfiguration _configuration = configuration;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly ILogger<CommentNotifier> _logger = logger;
    private readonly IClock _clock = clock;

    private const string LogLabel = "Comment-Notify";

    public async Task<int> NotifyAsync(CancellationToken cancellationToken = default)
    {
        var unsubscribeUrl = _configuration["Comments:UnsubscribeUrl"];
        if (string.IsNullOrWhiteSpace(unsubscribeUrl))
        {
            _logger.LogWarning("Yorum bildirimi gönderilemedi: Comments:UnsubscribeUrl yapılandırılmamış.");
            return 0;
        }

        var postUrlFormat = _configuration["Comments:PostUrl"];
        if (string.IsNullOrWhiteSpace(postUrlFormat) || !postUrlFormat.Contains("{slug}"))
        {
            _logger.LogWarning("Yorum bildirimi gönderilemedi: Comments:PostUrl yapılandırılmamış ya da {{slug}} yer tutucusu yok.");
            return 0;
        }

        var processed = 0;
        var cursor = 0;
        var tally = new MailTally();

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = (await _unitOfWork.CommentNotifications.GetAllPagedAsync(
                1, BatchSize,
                n => n.Status == CommentNotificationStatuses.Pending
                     && n.AttemptCount < MaxAttempts
                     && n.Id > cursor,
                false, cancellationToken)).ToList();

            if (batch.Count == 0) break;

            foreach (var notification in batch)
            {
                cursor = notification.Id;
                await AttemptAsync(notification, unsubscribeUrl!, postUrlFormat!, tally, cancellationToken);
                processed++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (!tally.IsEmpty)
            await _activityLogger.LogMailAsync($"Yorum yanıtı bildirim turu bitti. {tally.Summary()}", tally.HasFailures, LogLabel, cancellationToken);

        return processed;
    }

    private async Task AttemptAsync(CommentNotification notification, string unsubscribeUrl, string postUrlFormat, MailTally tally, CancellationToken cancellationToken)
    {
        var skip = await SkipReasonAsync(notification, cancellationToken);
        if (skip.Reason is not null)
        {
            notification.Status = CommentNotificationStatuses.Skipped;
            notification.Error = skip.Reason;
            await _unitOfWork.CommentNotifications.UpdateAsync(notification, cancellationToken);
            tally.RecordSkipped();
            return;
        }

        var (reply, parent, blog) = (skip.Reply!, skip.Parent!, skip.Blog!);
        var token = GenerateToken();

        var sent = await _mailSender.SendAsync(
            MailTemplateDefinitions.CommentReply, AppSourceDefinitions.Blog, notification.Email,
            new CommentReplyMailDto
            {
                RecipientName = parent.AuthorName,
                PostTitle = blog.Title,
                PostUrl = $"{postUrlFormat.Replace("{slug}", Uri.EscapeDataString(blog.Slug ?? ""))}#yorum-{reply.Id}",
                ReplyAuthorName = reply.AuthorName,
                ReplyBody = reply.Body,
                ReplyDate = reply.CreatedAt.ToString("dd.MM.yyyy HH:mm") + " (UTC)",
                Email = notification.Email,
                UnsubscribeUrl = $"{unsubscribeUrl}?token={Uri.EscapeDataString(token)}",
                ContactEmail = _configuration["Contact:ContactEmail"] ?? "",
                CurrentYear = _clock.UtcNow.Year.ToString()
            },
            cancellationToken);

        notification.AttemptCount++;

        if (sent.Success)
        {
            notification.Status = CommentNotificationStatuses.Sent;
            notification.SentAt = _clock.UtcNow;
            notification.TokenHash = Hash(token);
            notification.Error = null;
            tally.RecordSent();
        }
        else
        {
            notification.Error = Truncate(MailLog.Reason(sent), 500);

            var final = notification.AttemptCount >= MaxAttempts;
            if (final)
                notification.Status = CommentNotificationStatuses.Failed;

            tally.RecordFailure(MailLog.Reason(sent), final);
        }

        await _unitOfWork.CommentNotifications.UpdateAsync(notification, cancellationToken);
    }

    /// <summary>Gönderim anında koşulları yeniden okur. Kuyruğa girdikten sonra değişmiş olabilecek her şey burada denetlenir; sebep dolu dönerse posta gitmez ve satır atlanmış sayılır.<para>Yazının kendisi de denetlenir: yayından kalkmış bir yazıya çağıran bir bildirim, okuru 404'e götürürdü.</para></summary>
    private async Task<(string? Reason, Comment? Reply, Comment? Parent, Blog? Blog)> SkipReasonAsync(
        CommentNotification notification, CancellationToken cancellationToken)
    {
        var reply = await _unitOfWork.Comments.GetByIdForAdminAsync(notification.CommentId, cancellationToken);
        if (reply is null || reply.IsDeleted || !reply.IsActive || reply.Status != CommentStatuses.Approved)
            return ("Yanıt artık yayında değil.", null, null, null);

        if (reply.ParentId is not { } parentId)
            return ("Yanıtın bağlı olduğu yorum yok.", null, null, null);

        var parent = await _unitOfWork.Comments.GetByIdForAdminAsync(parentId, cancellationToken);
        if (parent is null || parent.IsDeleted || !parent.IsActive)
            return ("Yanıtlanan yorum artık yayında değil.", null, null, null);

        if (!parent.NotifyOnReply)
            return ("Alıcı yanıt bildirimlerini kapattı.", null, null, null);

        var blog = await _unitOfWork.Blogs.GetByIdAsync(reply.BlogId, cancellationToken);
        if (blog is null || string.IsNullOrWhiteSpace(blog.Slug))
            return ("Yazı artık yayında değil.", null, null, null);

        return (null, reply, parent, blog);
    }

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Hash(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
