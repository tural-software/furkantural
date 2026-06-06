namespace FurkanTural_Application.DTOs.ChatMessage;

/// <summary>
/// Bir arkadaşla olan konuşmanın özeti (son mesaj + okunmamış sayısı).
/// </summary>
public class ConversationSummaryDto
{
    public int FriendUserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
