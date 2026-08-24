using FurkanTural_Application.DTOs.Auth;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Kimlik doğrulama ve JWT üretimi. İki ayrı token türü vardır: LoginAsync/RegisterAsync gerçek kullanıcı için rol claim'i taşıyan token üretir, GenerateAppTokenAsync ise oturumu olmayan ön-yüzler için Visitor rolü ve app_source claim'i taşıyan uzun ömürlü uygulama token'ı üretir (bkz. <see cref="Settings.AppTokenSettings"/>). LoginDto'daki AppSource salt etiket değildir: Turnstile doğrulaması yalnızca yapılandırmada listelenen kaynaklar için zorunlu tutulur ve aynı değer token'a app_source olarak yazılır. RefreshAsync bu değeri parametre olarak alır, token'dan kendisi okumaz — çağıran taşımazsa claim yenilenen token'da kaybolur. Art arda hatalı girişte LoginAsync 401 değil 429 döner (bkz. <see cref="ILoginThrottle"/>).<para>ipAddress ve userAgent bilerek DTO'da değil ayrı parametrededir: DTO istemcinin gönderdiğidir, bu ikisi ise sunucunun gözlemlediğidir. Pasif bir hesabın açılma isteği bu ikisiyle birlikte kaydedilir (bkz. <see cref="IAccountActivationService"/>), dolayısıyla istemcinin yazabildiği bir alandan gelmeleri kaydı delil olmaktan çıkarırdı.</para><para>Pasif hesap her iki uçta da doğrulama postası tetikler ve yeni satır açılmaz. Giriş bunu ancak parola doğrulandıktan sonra yapar, aksi hâlde uç herhangi birinin istediği adrese posta yollatabildiği bir mekanizmaya dönerdi. Kayıt ise adresin dolu olduğunu zaten söylediği için ek bir şey ele vermez; üç durum da (etkin, pasif, silinmiş) aynı metni alır.</para></summary>
public interface IAuthService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<Result<LoginResultDto>> GenerateAppTokenAsync(AppTokenRequestDto dto, CancellationToken cancellationToken = default);
    Task<Result<LoginResultDto>> RegisterAsync(RegisterDto dto, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<Result<LoginResultDto>> RefreshAsync(int userId, string? appSource, CancellationToken cancellationToken = default);
}
