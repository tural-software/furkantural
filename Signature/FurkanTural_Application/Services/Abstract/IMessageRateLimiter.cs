namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Mesaj gönderimi (text/ses/medya) için kullanıcı başına hız sınırı — flood koruması.
/// In-process (tek instance) çalışır; bkz. ICallRateLimiter ile aynı desen.
/// </summary>
public interface IMessageRateLimiter
{
    /// <summary>Pencere içinde kota varsa kaydeder ve true döner; aşıldıysa false.</summary>
    bool TryRegisterSend(int userId);
}
