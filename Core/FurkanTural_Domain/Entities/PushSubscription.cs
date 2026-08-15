using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Bir <see cref="User"/>'ın tek bir tarayıcı/cihaz için Web Push aboneliği; kullanıcı başına
/// birden çok kayıt olabilir. Endpoint aboneliğin kimliğidir (tarayıcının push servis adresi),
/// P256dh ve Auth ise bildirim gövdesini şifrelemek için istemciden gelen anahtarlardır.
/// </summary>
public class PushSubscription : BaseEntity
{
    public int UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}
