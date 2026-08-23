namespace FurkanTural_Application.DTOs.ChatMessage;

/// <summary>Sohbet listesinin ham sayıları; doğrudan istemciye verilmez. Kullanıcı bilgisi taşımaz ve LastMessage bu aşamada hâlâ şifrelidir — çözme ile kullanıcı bilgisinin eklenmesi <see cref="ConversationSummaryDto"/> kurulurken yapılır.</summary>
public class ConversationAggregateDto
{
    public int OtherUserId { get; set; }
    public string? LastMessage { get; set; }
    public string? LastMessageType { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
