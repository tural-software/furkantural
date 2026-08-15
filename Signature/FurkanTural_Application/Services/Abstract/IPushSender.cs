namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Kullanıcı uygulamada değilken gönderilen tarayıcı push bildirimi. En iyi çaba ilkesiyle çalışır:
/// yapılandırma yoksa, abonelik yoksa veya gönderim başarısız olursa istisna fırlatmaz, sessizce
/// döner — mesajlaşmanın kendisi bildirime bağlı kalmasın diye. Geçersiz hâle gelmiş abonelikler bu
/// sırada temizlenir.
/// </summary>
public interface IPushSender
{
    Task SendMessageNotificationAsync(int receiverUserId, string senderName, CancellationToken cancellationToken = default);
}