namespace FurkanTural_Application.DTOs.ChatMessage;

/// <summary>Sohbet listesi satırı. Liste mesajlardan değil arkadaş listesinden kurulur: arkadaş olunmayan biriyle geçmişte yazışılmış olsa bile o sohbet listede görünmez, arkadaşlık kaldırıldığında da listeden düşer. Tersi de geçerli — hiç yazışılmamış arkadaş, alanları boş ve UnreadCount sıfır olarak yer alır. Sıralama son mesaj tarihine göredir, hiç mesajı olmayanlar en sona düşer.</summary>
public class ConversationSummaryDto
{
    public int FriendUserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? LastMessage { get; set; }
    public string? LastMessageType { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
