using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Jeton 32 bayt <see cref="RandomNumberGenerator"/> çıktısının URL'de güvenli base64'üdür; bağlantıda sorgu değeri olarak taşınacağı için standart base64'ün doldurma ve eğik çizgi karakterleri kullanılmaz. Saklanan değer bunun tuzsuz SHA-256 özetidir: tuz aranabilirliği bozardı ve girdi zaten 256 bit rastgele olduğu için sözlük saldırısına açık değildir — parola özetleyicisinin PBKDF2'si burada yanlış araçtır.<para>Jetonun düz hâli yalnızca giden postanın içinde bulunur; ne çağırana döner ne de kayda yazılır. Bu yüzden üretim ile gönderim tek metottadır: ikisini ayırmak, jetonu servis sınırının dışına taşımak demek olurdu.</para><para>Bağlantının adresi <c>Activation:LandingUrl</c>'den okunur ve yoksa posta hiç gönderilmez. Yapılandırılmamış bir adresle üretilen bağlantı kullanıcıyı hiçbir yere götürmez; jetonu harcamadan başarısız olmak, çalışmayan bir bağlantı yollamaktan iyidir.</para><para>Süresi geçmiş ya da harcanmış jeton silinmez, ayrı hatalarla reddedilir. Bağlantıyı elinde tutan kişi zaten meşru kabul edilir, dolayısıyla "süresi doldu" ile "zaten kullanıldı" ayrımı bir hesabın varlığını ele vermez; ayırmamak yalnızca kullanıcıyı ne yapacağını bilmez hâlde bırakırdı.</para><para>Aynı hesap için beş dakika içinde üretilmiş, henüz harcanmamış bir bağlantı varsa yenisi üretilmez ve sonuç yine başarılı döner. Tetikleyici doğru parolanın arkasında olsa da her denemede posta yollamak, hesabın sahibini kendi gelen kutusunda boğmanın yolu olurdu; duran bağlantı zaten yirmi dört saat geçerli olduğu için bekleyen kullanıcı bir şey kaybetmez.</para><para>Kullanıcı okuması küresel süzgecin arkasından geçmek zorundadır: aktifleştirilecek hesap tanımı gereği pasiftir ve süzgeçli okuma onu hiç görmez.</para></summary>
public class AccountActivationService(
    IUnitOfWork unitOfWork,
    IMailSender mailSender,
    IConfiguration configuration,
    ActivityLogger activityLogger,
    IClock clock) : IAccountActivationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMailSender _mailSender = mailSender;
    private readonly IConfiguration _configuration = configuration;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly IClock _clock = clock;

    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    public async Task<Result> IssueAsync(int userId, string triggerSource, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdForAdminAsync(userId, cancellationToken);
        if (user is null || user.IsDeleted)
            return Result.Fail("Hesap bulunamadı.", $"Aktivasyon üretilemedi: #{userId} yok ya da silinmiş.", 404);

        if (user.DeactivatedByAdmin)
            return Result.Fail("Bu hesap yönetici tarafından kapatıldı.", $"Aktivasyon üretilemedi: #{userId} yönetici yasağında.", 403);

        if (string.IsNullOrWhiteSpace(user.Email))
            return Result.Fail("Hesaba bağlı bir e-posta adresi yok.", $"Aktivasyon üretilemedi: #{userId} adressiz.");

        var mail = $"Aktivasyon postası (kullanıcı #{user.Id}, tetikleyen: {triggerSource})";

        var landingUrl = _configuration["Activation:LandingUrl"];
        if (string.IsNullOrWhiteSpace(landingUrl))
        {
            await _activityLogger.LogMailAsync(MailLog.Failed(mail, "Activation:LandingUrl yapılandırılmamış"), failed: true, cancellationToken);
            return Result.Fail("Aktivasyon gönderilemedi.", "Activation:LandingUrl yapılandırılmamış.", 500);
        }

        var now = _clock.UtcNow;
        var cutoff = now.Subtract(Cooldown);
        var pending = await _unitOfWork.AccountActivations
            .GetAsync(x => x.UserId == user.Id && x.ConsumedAt == null && x.CreatedAt > cutoff && x.ExpiresAt > now, cancellationToken);

        if (pending is not null)
        {
            await _activityLogger.LogAsync(
                MailLog.Skipped(mail, $"{Cooldown.TotalMinutes:0} dakika içinde üretilmiş, henüz harcanmamış bir bağlantı var"),
                cancellationToken);
            return Result.Ok();
        }

        var token = GenerateToken();
        var expiresAt = _clock.UtcNow.Add(Lifetime);

        var activation = new AccountActivation
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAt = expiresAt,
            RequestIpAddress = Truncate(ipAddress, 45),
            RequestUserAgent = Truncate(userAgent, 300),
            TriggerSource = Truncate(triggerSource, 50)
        };

        await _unitOfWork.AccountActivations.AddAsync(activation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var sent = await _mailSender.SendAsync(MailTemplateDefinitions.AccountActivation, AppSourceDefinitions.Chat, user.Email, new AccountActivationMailDto
        {
            DisplayName = user.DisplayName ?? user.Username,
            ActivationUrl = $"{landingUrl}?token={Uri.EscapeDataString(token)}",
            ExpiresAt = expiresAt.ToString("dd.MM.yyyy HH:mm") + " (UTC)",
            IpAddress = ipAddress,
            Browser = userAgent,
            ContactEmail = _configuration["Contact:ContactEmail"] ?? "",
            CurrentYear = _clock.UtcNow.Year.ToString()
        }, cancellationToken);

        if (sent.IsFailure)
        {
            activation.ExpiresAt = _clock.UtcNow;
            await _unitOfWork.AccountActivations.UpdateAsync(activation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _activityLogger.LogMailAsync(MailLog.Describe(mail, sent), sent.IsFailure, cancellationToken);

        return sent.IsFailure
            ? Result.Fail("Aktivasyon gönderilemedi.", sent.InternalMessage, sent.StatusCode)
            : Result.Ok();
    }

    public async Task<Result> ConsumeAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Fail("Doğrulama bağlantısı geçersiz.");

        var activation = await _unitOfWork.AccountActivations
            .GetAsync(x => x.TokenHash == Hash(token), cancellationToken);

        if (activation is null)
            return Result.Fail("Doğrulama bağlantısı geçersiz.");

        if (activation.ConsumedAt is not null)
            return Result.Fail("Bu doğrulama bağlantısı daha önce kullanılmış.", statusCode: 410);

        if (activation.ExpiresAt <= _clock.UtcNow)
            return Result.Fail("Doğrulama bağlantısının süresi dolmuş. Lütfen yeniden deneyin.", statusCode: 410);

        var user = await _unitOfWork.Users.GetByIdForAdminAsync(activation.UserId, cancellationToken);
        if (user is null || user.IsDeleted)
            return Result.Fail("Bu hesap açılamaz.", $"Aktivasyon reddedildi: #{activation.UserId} yok ya da silinmiş.");

        if (user.DeactivatedByAdmin)
            return Result.Fail("Bu hesap yönetici tarafından kapatıldı. Bilgi için destek@furkantural.com adresine yazın.",
                $"Aktivasyon reddedildi: #{activation.UserId} yönetici yasağında.", 403);

        if (!await _unitOfWork.TryConsumeTokenAsync<AccountActivation>(activation.Id, _clock.UtcNow, cancellationToken))
            return Result.Fail("Bu doğrulama bağlantısı daha önce kullanılmış.",
                $"Aktivasyon reddedildi: #{activation.Id} jetonu bu istek okurken başka bir istek tarafından harcanmış.", 410);

        activation.ConsumedAt = _clock.UtcNow;

        if (!user.IsActive)
        {
            AccountClosure.Reopen(user);
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok("Hesabınız yeniden etkinleştirildi. Artık giriş yapabilirsiniz.");
    }

    private static string GenerateToken()
        => Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
