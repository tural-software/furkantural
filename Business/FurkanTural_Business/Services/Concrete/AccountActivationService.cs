using System.Security.Cryptography;
using System.Text;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Jeton 32 bayt <see cref="RandomNumberGenerator"/> çıktısının URL'de güvenli base64'üdür; bağlantıda sorgu değeri olarak taşınacağı için standart base64'ün doldurma ve eğik çizgi karakterleri kullanılmaz. Saklanan değer bunun tuzsuz SHA-256 özetidir: tuz aranabilirliği bozardı ve girdi zaten 256 bit rastgele olduğu için sözlük saldırısına açık değildir — parola özetleyicisinin PBKDF2'si burada yanlış araçtır.<para>Süresi geçmiş ya da harcanmış jeton silinmez, ayrı hatalarla reddedilir. Bağlantıyı elinde tutan kişi zaten meşru kabul edilir, dolayısıyla "süresi doldu" ile "zaten kullanıldı" ayrımı bir hesabın varlığını ele vermez; ayırmamak yalnızca kullanıcıyı ne yapacağını bilmez hâlde bırakırdı.</para><para>Kullanıcı okuması küresel süzgecin arkasından geçmek zorundadır: aktifleştirilecek hesap tanımı gereği pasiftir ve süzgeçli okuma onu hiç görmez.</para></summary>
public class AccountActivationService(IUnitOfWork unitOfWork, IClock clock) : IAccountActivationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IClock _clock = clock;

    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public async Task<Result<string>> IssueAsync(int userId, string triggerSource, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdForAdminAsync(userId, cancellationToken);
        if (user is null || user.IsDeleted)
            return Result<string>.Fail("Hesap bulunamadı.", $"Aktivasyon üretilemedi: #{userId} yok ya da silinmiş.", 404);

        var token = GenerateToken();

        await _unitOfWork.AccountActivations.AddAsync(new AccountActivation
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAt = _clock.UtcNow.Add(Lifetime),
            RequestIpAddress = Truncate(ipAddress, 45),
            RequestUserAgent = Truncate(userAgent, 300),
            TriggerSource = Truncate(triggerSource, 50)
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<string>.Ok(token);
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

        activation.ConsumedAt = _clock.UtcNow;
        await _unitOfWork.AccountActivations.UpdateAsync(activation, cancellationToken);

        if (!user.IsActive)
        {
            user.IsActive = true;
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
