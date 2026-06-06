namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Cloudflare Turnstile token doğrulaması için paylaşılan soyutlama.
/// SecretKey API config'inde (şifreli) durur; doğrulama tek noktada yapılır.
/// </summary>
public interface ITurnstileVerifier
{
    /// <summary>
    /// Turnstile token'ını doğrular. SecretKey yapılandırılmamışsa (boş/placeholder)
    /// doğrulama atlanır ve true döner.
    /// </summary>
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default);
}
