using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>İki kullanıcı arasındaki birebir mesaj; SenderId ve ReceiverId <see cref="User"/>'a bakar, gönderim anı CreatedAt'tir. Content veritabanında AES-GCM ile şifreli durur, düz metne yalnızca serviste dönülür. MessageType <see cref="Constants.ChatMessageTypes"/> sabitlerinden gelir; AttachmentUrl adres değil, yükleme klasörüne göreli dosya adıdır. EditedAt ayrı tutulur, çünkü UpdatedAt'i okundu işaretlemesi de damgalar.</summary>
public class ChatMessage : BaseEntity
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string? Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public string? MessageType { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? DurationSeconds { get; set; }

    public DateTime? EditedAt { get; set; }
}
