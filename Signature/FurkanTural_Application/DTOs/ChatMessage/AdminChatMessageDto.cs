namespace FurkanTural_Application.DTOs.ChatMessage;

/// <summary>Mesajın yönetim paneli görünümü. Content burada da çözülmüş olarak döner, yani yönetici kullanıcıların yazışmalarını düz metin okur; şifreleme yalnızca veri tabanındaki saklamayı korur.</summary>
public class AdminChatMessageDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string? SenderUsername { get; set; }
    public string? ReceiverUsername { get; set; }
    public string? Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? MessageType { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
