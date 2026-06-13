namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Web Push ile (uygulama kapalıyken bile) bildirim gönderir. Bildirim "en iyi çaba"dır:
/// yapılandırma yoksa veya gönderim başarısızsa <b>sessizce</b> geçer, asıl akışı (mesaj gönderimi) bozmaz.
/// </summary>
public interface IPushSender
{
    /// <summary>Alıcıya "yeni mesaj" bildirimi gönderir (yalnız gönderen adı; içerik gizli).</summary>
    Task SendMessageNotificationAsync(int receiverUserId, string senderName, CancellationToken cancellationToken = default);
}
