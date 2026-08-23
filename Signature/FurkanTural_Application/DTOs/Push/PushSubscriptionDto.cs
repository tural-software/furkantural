namespace FurkanTural_Application.DTOs.Push;

/// <summary>Tarayıcının Push API'sinden dönen abonelik nesnesi. P256dh ile Auth, bildirim gövdesinin şifrelenmesi için tarayıcının ürettiği anahtarlardır; sunucu bunları yorumlamaz, olduğu gibi saklar. Kimlik alanı Endpoint'tir: aynı endpoint yeniden gönderildiğinde yeni kayıt açılmaz, mevcut satır güncellenir ve cihaz başka bir hesaba geçmiş olabileceği için sahiplik de yeniden yazılır.</summary>
public class PushSubscriptionDto
{
    public string? Endpoint { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? UserAgent { get; set; }
}
