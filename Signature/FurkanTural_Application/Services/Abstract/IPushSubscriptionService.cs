using FurkanTural_Application.DTOs.Push;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Tarayıcı push abonelikleri. Kimlik kullanıcı değil endpoint'tir: aynı endpoint yeniden gönderilirse kayıt güncellenir ve gerekirse başka bir kullanıcıya devredilir, çünkü aynı cihaz farklı hesaba geçmiş olabilir. UnsubscribeAsync burada yumuşak değil kalıcı siler ve kayıt bulunamasa da başarı döner. GetVapidPublicKey yapılandırma eksik ya da yer tutucuysa null verir; istemci bunu görüp abone olmayı hiç denemez.</summary>
public interface IPushSubscriptionService
{
    Task<Result> SubscribeAsync(int userId, PushSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task<Result> UnsubscribeAsync(int userId, string? endpoint, CancellationToken cancellationToken = default);
    string? GetVapidPublicKey();
}
