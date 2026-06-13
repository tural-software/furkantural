namespace FurkanTural_Application.DTOs.Push;

/// <summary>Tarayıcının ürettiği Web Push aboneliği (PushSubscription.toJSON düzleştirilmiş hâli).</summary>
public class PushSubscriptionDto
{
    public string? Endpoint { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? UserAgent { get; set; }
}
