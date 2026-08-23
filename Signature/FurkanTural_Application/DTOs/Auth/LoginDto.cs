namespace FurkanTural_Application.DTOs.Auth;

/// <summary>Kullanıcı girişi. AppSource hangi ön-yüzün istediğini bildirir; boş değilse token'a <c>app_source</c> claim'i olarak yazılır ve Turnstile'ın bu giriş için zorunlu olup olmadığını da o belirler (<c>Turnstile:RequiredApps</c> listesinde aranır). Dikkat: AppSource boş geldiğinde liste hiç sorgulanmaz ve bot doğrulaması atlanır, yani TurnstileToken'ın zorunluluğu istemcinin bildirdiği ada bağlıdır.</summary>
public class LoginDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? AppSource { get; set; }
    public string? TurnstileToken { get; set; }
}
