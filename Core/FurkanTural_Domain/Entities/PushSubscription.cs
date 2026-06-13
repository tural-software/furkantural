using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Bir kullanıcının bir cihaz/tarayıcı için Web Push aboneliği. Uygulama kapalıyken bile
/// (service worker üzerinden) bildirim ulaştırmak için push servisi adresi ve şifreleme anahtarlarını tutar.
/// Bir kullanıcının birden çok cihazı olabilir → kullanıcı başına çok kayıt.
/// </summary>
public class PushSubscription : BaseEntity
{
    public int UserId { get; set; }

    /// <summary>Tarayıcının push servisi adresi (FCM/Apple/Mozilla). Aboneliğin benzersiz kimliği.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>İstemci açık anahtarı (içerik şifrelemesi için).</summary>
    public string P256dh { get; set; } = string.Empty;

    /// <summary>İstemci auth secret'ı (içerik şifrelemesi için).</summary>
    public string Auth { get; set; } = string.Empty;

    /// <summary>Cihaz tanımlama (yönetim/teşhis için; zorunlu değil).</summary>
    public string? UserAgent { get; set; }
}
