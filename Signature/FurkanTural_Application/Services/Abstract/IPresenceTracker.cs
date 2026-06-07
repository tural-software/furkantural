namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Çevrimiçi kullanıcıları bellek içinde izler (SignalR bağlantı sayımı, çoklu sekme güvenli).
/// Singleton olarak kaydedilir; kalıcı değildir (uygulama yeniden başlayınca sıfırlanır).
/// </summary>
public interface IPresenceTracker
{
    /// <summary>Bağlantıyı kaydeder. Kullanıcının ilk aktif bağlantısıysa <c>true</c> döner (çevrimiçi oldu).</summary>
    bool Connect(int userId, string connectionId);

    /// <summary>Bağlantıyı kaldırır. Kullanıcının son bağlantısıysa <c>true</c> döner (çevrimdışı oldu).</summary>
    bool Disconnect(int userId, string connectionId);

    /// <summary>Kullanıcının en az bir aktif bağlantısı var mı?</summary>
    bool IsOnline(int userId);
}
