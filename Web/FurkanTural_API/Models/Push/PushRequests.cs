namespace FurkanTural_API.Models.Push;

// Adı kısaltılıp SubscribeRequest yapılamaz: Subscriber altındaki tiple çakışır ve derleme
// geçse bile swagger.json üretimi 500 döner.
public class PushSubscriptionRequest
{
    public string? Endpoint { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? UserAgent { get; set; }
}

public class UnsubscribeRequest
{
    public string? Endpoint { get; set; }
}