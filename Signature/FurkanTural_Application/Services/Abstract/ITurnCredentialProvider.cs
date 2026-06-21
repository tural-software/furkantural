using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// WebRTC için kısa ömürlü TURN (ICE) kimlik bilgileri üretir.
/// Cloudflare Realtime TURN API'sini sunucuda çağırır; API token tarayıcıya verilmez.
/// </summary>
public interface ITurnCredentialProvider
{
    /// <param name="customIdentifier">Cloudflare kullanım analizi için kullanıcı kimliği (opsiyonel).</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için belirteç.</param>
    Task<Result<TurnCredentialsDto>> GetIceServersAsync(int? customIdentifier = null, CancellationToken cancellationToken = default);
}
