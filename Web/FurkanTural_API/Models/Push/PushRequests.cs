namespace FurkanTural_API.Models.Push;

// Not: Subscriber.SubscribeRequest ile aynı kısa ad → Swagger schemaId çakışması (CS yok ama
// swagger.json üretimi 500). Bu tip semantik olarak "push aboneliği" olduğundan ayrı adlandırıldı.
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
