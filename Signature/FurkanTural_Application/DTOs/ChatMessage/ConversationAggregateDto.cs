namespace FurkanTural_Application.DTOs.ChatMessage;

/// <summary>
/// Repository'nin tek sorguda döndürdüğü, karşı kullanıcı başına konuşma istatistiği
/// (son mesaj + okunmamış sayısı). Arkadaş meta verisiyle servis katmanında birleştirilir.
/// </summary>
public class ConversationAggregateDto
{
    public int OtherUserId { get; set; }
    public string? LastMessage { get; set; }
    public string? LastMessageType { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
