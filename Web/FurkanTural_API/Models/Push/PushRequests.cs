namespace FurkanTural_API.Models.Push;

public class SubscribeRequest
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
