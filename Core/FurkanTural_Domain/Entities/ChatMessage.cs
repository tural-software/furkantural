using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// İki kullanıcı arasında birebir sohbet mesajı. Gönderim zamanı = <c>CreatedAt</c>.
/// </summary>
public class ChatMessage : BaseEntity
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string? Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    /// <summary>"Text" (varsayılan) veya "Audio". Null/boş = Text.</summary>
    public string? MessageType { get; set; }
    /// <summary>Ses mesajı için yüklenen dosyanın adı (/images/uploads altında).</summary>
    public string? AttachmentUrl { get; set; }
    public int? DurationSeconds { get; set; }
}
