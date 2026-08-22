namespace FurkanTural_Admin.Models.ChatMessage;

public sealed class ChatMessageAdminDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string? SenderUsername { get; set; }
    public string? ReceiverUsername { get; set; }
    public string? Content { get; set; }
    public string? MessageType { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}