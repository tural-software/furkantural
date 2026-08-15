using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// WebRTC için kısa ömürlü ICE/TURN kimlik bilgileri. Değerler kalıcı değildir, her çağrıda dış
/// sağlayıcıdan yeniden üretilir ve süreleri dolar; saklanıp yeniden kullanılmamalıdır. Yapılandırma
/// eksikse 503 döner — bu geçici bir hata değil, arama altyapısının hiç kurulmamış olduğu anlamına
/// gelir. customIdentifier yalnızca sağlayıcı tarafındaki kullanım ölçümünü kullanıcıya bağlamak
/// içindir, yetkilendirmeye etki etmez.
/// </summary>
public interface ITurnCredentialProvider
{
    Task<Result<TurnCredentialsDto>> GetIceServersAsync(int? customIdentifier = null, CancellationToken cancellationToken = default);
}