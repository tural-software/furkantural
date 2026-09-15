using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.DTOs.Subscriber;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Jeton üretimi ve saklanması <see cref="AccountActivationService"/> ile birebir aynı kalıptadır: 32 bayt rastgeleden URL-güvenli base64, tabloya tuzsuz SHA-256 özeti. Girdi zaten 256 bit rastgele olduğu için tuz aranabilirliği bozar, PBKDF2 ise burada yanlış araçtır.<para>Bağlantı adresleri yapılandırmadan okunur ve yoksa posta hiç gönderilmez: yapılandırılmamış bir adresle üretilen bağlantı kullanıcıyı hiçbir yere götürmez, jetonu harcamadan başarısız olmak çalışmayan bir bağlantı yollamaktan iyidir.</para><para>Abonelik ve çıkış uçları, adresin listede olup olmadığını ele vermemek için <b>her durumda aynı</b> metni ve durumu döndürür. Yalnızca içeride ne yapıldığı değişir; dışarıdan bakan biri için iki durum ayırt edilemez.</para><para>Beş dakikalık soğuma penceresi vardır: aynı adres için bekleyen bir bağlantı varken yenisi üretilmez ve sonuç yine başarılı döner. Aksi hâlde uç, herhangi birinin istediği adrese arka arkaya posta yollatabildiği bir mekanizmaya dönerdi.</para></summary>
public class NewsletterService(
    IUnitOfWork unitOfWork,
    IMailSender mailSender,
    ITurnstileVerifier turnstileVerifier,
    IConfiguration configuration,
    ActivityLogger activityLogger,
    ILogger<NewsletterService> logger,
    IClock clock) : INewsletterService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMailSender _mailSender = mailSender;
    private readonly ITurnstileVerifier _turnstileVerifier = turnstileVerifier;
    private readonly IConfiguration _configuration = configuration;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly ILogger<NewsletterService> _logger = logger;
    private readonly IClock _clock = clock;

    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    /// <summary>Adres kayıtlı olsun olmasın dönen tek metin. İki durumu ayırmak, uçları kimin abone olduğunu tek tek sınayabilen bir sorgulama aracına çevirirdi.</summary>
    private const string NeutralSubscribeMessage =
        "Adresinize bir doğrulama bağlantısı gönderdik. Bağlantıya tıklamadan listeye eklenmezsiniz.";

    private const string NeutralUnsubscribeMessage =
        "Adres listemizdeyse çıkış bağlantısını gönderdik. Bağlantıya tıklayarak çıkışı tamamlayabilirsiniz.";

    public async Task<Result> SubscribeAsync(string? email, string? turnstileToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (!await _turnstileVerifier.VerifyAsync(turnstileToken, ipAddress, cancellationToken))
            return Result.Fail("Bot doğrulaması başarısız. Lütfen tekrar deneyin.");

        var address = Normalize(email);
        if (address is null)
            return Result.Fail("Geçerli bir e-posta adresi girin.");

        var confirmUrl = _configuration["Newsletter:ConfirmUrl"];
        if (string.IsNullOrWhiteSpace(confirmUrl))
            return Result.Fail("Abonelik şu anda alınamıyor.", "Newsletter:ConfirmUrl yapılandırılmamış.", 500);

        var subscriber = await _unitOfWork.Subscribers.GetByEmailForAdminAsync(address, cancellationToken);

        if (subscriber is { IsDeleted: false, IsActive: true, ConfirmedAt: not null })
        {
            _logger.LogInformation("Abonelik postası gönderilmedi: adres zaten doğrulanmış.");
            return Result.Ok(NeutralSubscribeMessage);
        }

        if (subscriber is null)
        {
            subscriber = new CreateSubscriberDto { Email = address }.ToEntity();
            await _unitOfWork.Subscribers.AddAsync(subscriber, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _activityLogger.LogAsync($"Bülten kaydı açıldı, doğrulama bekleniyor. Id: {subscriber.Id}", cancellationToken);
        }
        else if (subscriber.IsDeleted || !subscriber.IsActive)
        {
            await _unitOfWork.Subscribers.RestoreAsync(subscriber, cancellationToken);
            subscriber.ConfirmedAt = null;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _activityLogger.LogAsync($"Bülten kaydı yeniden açıldı, doğrulama bekleniyor. Id: {subscriber.Id}", cancellationToken);
        }

        return await IssueAsync(
            subscriber, SubscriberVerificationPurposes.Confirm, MailTemplateDefinitions.NewsletterConfirm,
            confirmUrl, ipAddress, userAgent, NeutralSubscribeMessage, cancellationToken);
    }

    public async Task<Result> ConfirmAsync(string? token, CancellationToken cancellationToken = default)
    {
        var (verification, failure) = await ResolveAsync(token, SubscriberVerificationPurposes.Confirm, cancellationToken);
        if (failure is not null)
            return failure;

        var subscriber = await _unitOfWork.Subscribers.GetByIdForAdminAsync(verification!.SubscriberId, cancellationToken);
        if (subscriber is null)
            return Result.Fail("Bu bağlantı artık geçerli değil.", $"Abonelik onayı reddedildi: #{verification.SubscriberId} yok.");

        if (!await _unitOfWork.TryConsumeTokenAsync<SubscriberVerification>(verification.Id, _clock.UtcNow, cancellationToken))
            return Result.Fail("Bu bağlantı daha önce kullanılmış.",
                $"Abonelik onayı reddedildi: #{verification.Id} jetonu bu istek okurken başka bir istek tarafından harcanmış.", 410);

        verification.ConsumedAt = _clock.UtcNow;

        if (subscriber.IsDeleted || !subscriber.IsActive)
            await _unitOfWork.Subscribers.RestoreAsync(subscriber, cancellationToken);

        subscriber.ConfirmedAt ??= _clock.UtcNow;
        await _unitOfWork.Subscribers.UpdateAsync(subscriber, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Bülten aboneliği doğrulandı. Id: {subscriber.Id}", cancellationToken);

        return Result.Ok("Aboneliğiniz doğrulandı. Yeni yazıları duyurduğumuzda haberiniz olacak.");
    }

    public async Task<Result> RequestUnsubscribeAsync(string? email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var address = Normalize(email);
        if (address is null)
            return Result.Fail("Geçerli bir e-posta adresi girin.");

        var unsubscribeUrl = _configuration["Newsletter:UnsubscribeUrl"];
        if (string.IsNullOrWhiteSpace(unsubscribeUrl))
            return Result.Fail("İstek şu anda alınamıyor.", "Newsletter:UnsubscribeUrl yapılandırılmamış.", 500);

        var subscriber = await _unitOfWork.Subscribers.GetByEmailForAdminAsync(address, cancellationToken);
        if (subscriber is null || subscriber.IsDeleted || !subscriber.IsActive)
        {
            _logger.LogInformation("Çıkış postası gönderilmedi: adres listede değil.");
            return Result.Ok(NeutralUnsubscribeMessage);
        }

        return await IssueAsync(
            subscriber, SubscriberVerificationPurposes.Unsubscribe, MailTemplateDefinitions.NewsletterUnsubscribe,
            unsubscribeUrl, ipAddress, userAgent, NeutralUnsubscribeMessage, cancellationToken);
    }

    public async Task<Result> UnsubscribeAsync(string? token, CancellationToken cancellationToken = default)
    {
        var (verification, failure) = await ResolveAsync(token, SubscriberVerificationPurposes.Unsubscribe, cancellationToken);
        if (failure is not null)
            return failure;

        var subscriber = await _unitOfWork.Subscribers.GetByIdForAdminAsync(verification!.SubscriberId, cancellationToken);
        if (subscriber is null)
            return Result.Fail("Bu bağlantı artık geçerli değil.", $"Çıkış reddedildi: #{verification.SubscriberId} yok.");

        if (!await _unitOfWork.TryConsumeTokenAsync<SubscriberVerification>(verification.Id, _clock.UtcNow, cancellationToken))
            return Result.Fail("Bu bağlantı daha önce kullanılmış.",
                $"Çıkış reddedildi: #{verification.Id} jetonu bu istek okurken başka bir istek tarafından harcanmış.", 410);

        verification.ConsumedAt = _clock.UtcNow;

        if (!subscriber.IsDeleted)
            await _unitOfWork.Subscribers.SoftDeleteAsync(subscriber, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Bülten aboneliği iptal edildi. Id: {subscriber.Id}", cancellationToken);

        return Result.Ok("Aboneliğiniz iptal edildi. Bundan sonra bülten göndermeyeceğiz.");
    }

    /// <summary>Jeton üretir, kaydeder ve bağlantıyı postalar. Soğuma penceresi içinde bekleyen bir bağlantı varsa yenisi üretilmez ve sonuç yine <paramref name="neutralMessage"/> ile başarılı döner; duran bağlantı zaten yirmi dört saat geçerli olduğu için bekleyen kullanıcı bir şey kaybetmez.<para>Soğuma yalnızca kısa ömürlü, yani bu akışın kendi ürettiği bağlantıları sayar. Bültene gömülen çıkış bağlantıları çok daha uzun ömürlüdür ve bir posta isteğinin karşılığı değildir; onları da sayan bir pencere, bülteni yeni almış birinin çıkış isteğini sessizce yutardı.</para></summary>
    private async Task<Result> IssueAsync(
        Subscriber subscriber, string purpose, string mailType, string landingUrl,
        string? ipAddress, string? userAgent, string neutralMessage, CancellationToken cancellationToken)
    {
        var cutoff = _clock.UtcNow.Subtract(Cooldown);
        var horizon = _clock.UtcNow.Add(Lifetime);
        var pending = await _unitOfWork.SubscriberVerifications.GetAsync(
            x => x.SubscriberId == subscriber.Id && x.Purpose == purpose && x.ConsumedAt == null
                 && x.CreatedAt > cutoff && x.ExpiresAt <= horizon,
            cancellationToken);

        if (pending is not null)
        {
            _logger.LogInformation(
                "Bülten postası gönderilmedi: #{SubscriberId} için {Minutes} dakika içinde üretilmiş, henüz harcanmamış bir {Purpose} bağlantısı var.",
                subscriber.Id, Cooldown.TotalMinutes, purpose);
            return Result.Ok(neutralMessage);
        }

        var token = GenerateToken();
        var expiresAt = _clock.UtcNow.Add(Lifetime);

        await _unitOfWork.SubscriberVerifications.AddAsync(new SubscriberVerification
        {
            SubscriberId = subscriber.Id,
            TokenHash = Hash(token),
            Purpose = purpose,
            ExpiresAt = expiresAt,
            RequestIpAddress = Truncate(ipAddress, 45),
            RequestUserAgent = Truncate(userAgent, 300)
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var url = $"{landingUrl}?token={Uri.EscapeDataString(token)}";
        var expires = expiresAt.ToString("dd.MM.yyyy HH:mm") + " (UTC)";
        var contactEmail = _configuration["Contact:ContactEmail"] ?? "";
        var year = _clock.UtcNow.Year.ToString();

        object payload = purpose == SubscriberVerificationPurposes.Confirm
            ? new NewsletterConfirmMailDto
            {
                Email = subscriber.Email,
                ConfirmUrl = url,
                ExpiresAt = expires,
                IpAddress = ipAddress,
                Browser = userAgent,
                ContactEmail = contactEmail,
                CurrentYear = year
            }
            : new NewsletterUnsubscribeMailDto
            {
                Email = subscriber.Email,
                UnsubscribeUrl = url,
                ExpiresAt = expires,
                IpAddress = ipAddress,
                Browser = userAgent,
                ContactEmail = contactEmail,
                CurrentYear = year
            };

        var sent = await _mailSender.SendAsync(mailType, AppSourceDefinitions.Blog, subscriber.Email, payload, cancellationToken);

        return sent.IsFailure
            ? Result.Fail("Posta gönderilemedi. Kısa süre sonra tekrar deneyin.", sent.InternalMessage, sent.StatusCode)
            : Result.Ok(neutralMessage);
    }

    /// <summary>Jetonu bulur ve kullanılabilirliğini denetler. Amaç eşleşmesi ayrı bir koşuldur: çıkış için üretilmiş bir bağlantı aboneliği onaylayamamalı, onay bağlantısı da listeden düşürememelidir.</summary>
    private async Task<(SubscriberVerification? Verification, Result? Failure)> ResolveAsync(
        string? token, string purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (null, Result.Fail("Bağlantı geçersiz."));

        var hash = Hash(token);
        var verification = await _unitOfWork.SubscriberVerifications.GetAsync(
            x => x.TokenHash == hash && x.Purpose == purpose, cancellationToken);

        if (verification is null)
            return (null, Result.Fail("Bağlantı geçersiz."));

        if (verification.ConsumedAt is not null)
            return (null, Result.Fail("Bu bağlantı daha önce kullanılmış.", statusCode: 410));

        if (verification.ExpiresAt <= _clock.UtcNow)
            return (null, Result.Fail("Bağlantının süresi dolmuş. Lütfen yeniden deneyin.", statusCode: 410));

        return (verification, null);
    }

    /// <summary>Adresi kırpar, küçük harfe indirir ve biçimini denetler. Geçersizse null döner; adres tekilliği bu değere dayandığı için aynı adresin iki farklı yazımı iki kayıt açmamalıdır.</summary>
    private static string? Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var trimmed = email.Trim();
        if (trimmed.Length > 256 || !MailAddress.TryCreate(trimmed, out var parsed) || parsed.Address != trimmed)
            return null;

        return trimmed.ToLowerInvariant();
    }

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Hash(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
