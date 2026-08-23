namespace FurkanTural_Application.DTOs.Call;

/// <summary>Arama geçmişi satırının, isteği yapan kullanıcıya göre düzleştirilmiş hâli. Kayıtta arayan ve aranan ayrı alanlarda durur; buradaki Other* alanları ile Direction ise çağırana göre hesaplanır, dolayısıyla aynı arama iki taraf için farklı DTO üretir.</summary>
public class CallLogDto
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string? OtherUsername { get; set; }
    public string? OtherDisplayName { get; set; }
    public string? OtherAvatarUrl { get; set; }
    public string Direction { get; set; } = "Outgoing";
    public string? CallType { get; set; }
    public string? Status { get; set; }
    public DateTime StartedAt { get; set; }
    public int? DurationSeconds { get; set; }
}
