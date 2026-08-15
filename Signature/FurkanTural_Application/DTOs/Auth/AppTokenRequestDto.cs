namespace FurkanTural_Application.DTOs.Auth;

/// <summary>
/// Kullanıcı oturumu olmayan ön-yüzün kendi adına token istemesi. İki alan birlikte eşleşmelidir;
/// karşılığı <see cref="Settings.AppTokenSettings"/> içindeki kayıt listesidir.
/// </summary>
public class AppTokenRequestDto
{
    public string? AppKey { get; set; }
    public string? AppName { get; set; }
}