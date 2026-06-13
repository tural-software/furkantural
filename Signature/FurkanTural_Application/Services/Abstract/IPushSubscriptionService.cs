using FurkanTural_Application.DTOs.Push;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Web Push aboneliklerini yönetir (cihaz başına kaydet/sil) ve istemci için VAPID açık anahtarını verir.</summary>
public interface IPushSubscriptionService
{
    Task<Result> SubscribeAsync(int userId, PushSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task<Result> UnsubscribeAsync(int userId, string? endpoint, CancellationToken cancellationToken = default);

    /// <summary>İstemcinin pushManager.subscribe için kullanacağı VAPID açık anahtarı (gizli değil). Yapılandırılmamışsa null.</summary>
    string? GetVapidPublicKey();
}
