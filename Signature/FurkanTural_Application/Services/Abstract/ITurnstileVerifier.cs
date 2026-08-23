namespace FurkanTural_Application.Services.Abstract;

/// <summary>Cloudflare Turnstile bot doğrulaması. Dikkat: gizli anahtar yapılandırılmamışsa hiç doğrulama yapılmadan true döner — geliştirme ortamında formlar kilitlenmesin diyedir, ama üretimde eksik yapılandırma korumayı sessizce kaldırır. Anahtar varken ağ hatası veya geçersiz yanıt false'a düşer, yani yalnızca yapılandırılmış hâlde kapalı-devre çalışır.</summary>
public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default);
}
