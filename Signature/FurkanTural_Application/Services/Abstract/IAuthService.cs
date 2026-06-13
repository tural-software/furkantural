using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IAuthService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<Result<LoginResultDto>> GenerateAppTokenAsync(AppTokenRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>Yeni üye kaydı oluşturur ve kayıt sonrası otomatik giriş token'ı döner.</summary>
    Task<Result<LoginResultDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Geçerli (süresi henüz dolmamış) bir kullanıcı token'ını aynı kimlik ve app_source ile yeniler.
    /// BFF oturumu (8 saat) sürerken 60 dakikalık JWT yüzünden kullanıcı düşmesin diye kullanılır.
    /// </summary>
    Task<Result<LoginResultDto>> RefreshAsync(int userId, string? appSource, CancellationToken cancellationToken = default);
}
