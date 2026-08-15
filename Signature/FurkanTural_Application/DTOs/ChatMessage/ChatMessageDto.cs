namespace FurkanTural_Application.DTOs.ChatMessage;

/// <summary>
/// Sohbet mesajının istemciye verilen hâli. Content burada düz metindir; veri tabanında şifreli durur ve
/// yalnızca bu DTO'ya çevrilirken çözülür. EditedAt yalnızca düzenlenmiş mesajlarda dolar, düzenleme de
/// gönderimden sonraki 15 dakikayla ve yalnızca metin mesajlarıyla sınırlıdır.
/// </summary>
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
    public DateTime? EditedAt { get; set; }
}