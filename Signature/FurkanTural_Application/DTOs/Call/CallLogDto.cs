namespace FurkanTural_Application.DTOs.Call;

/// <summary>Üye arama geçmişi öğesi (karşı taraf bilgisiyle zenginleştirilmiş).</summary>
public class CallLogDto
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string? OtherUsername { get; set; }
    public string? OtherDisplayName { get; set; }
    public string? OtherAvatarUrl { get; set; }
    /// <summary>"Incoming" | "Outgoing".</summary>
    public string Direction { get; set; } = "Outgoing";
    public string? CallType { get; set; }
    public string? Status { get; set; }
    public DateTime StartedAt { get; set; }
    public int? DurationSeconds { get; set; }
}
