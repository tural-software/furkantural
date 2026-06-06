namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Arama başlatma hız sınırı (arama-spam / taciz önleme). Singleton, in-memory kayan pencere.
/// </summary>
public interface ICallRateLimiter
{
    /// <summary>Bu kullanıcı şu an yeni bir arama başlatabilir mi? Başlatabiliyorsa kaydeder ve <c>true</c> döner.</summary>
    bool TryStartCall(int userId);
}
