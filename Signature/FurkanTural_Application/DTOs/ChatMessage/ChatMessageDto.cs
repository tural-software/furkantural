namespace FurkanTural_Application.DTOs.ChatMessage;

public class ChatMessageDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? MessageType { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? DurationSeconds { get; set; }
    /// <summary>Mesajın son düzenlenme anı (yalnızca Text); null = düzenlenmedi.</summary>
    public DateTime? EditedAt { get; set; }
}
