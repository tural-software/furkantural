namespace FurkanTural_Application.DTOs.UserFriend;

/// <summary>
/// Onaylanmış bir arkadaşı temsil eder (karşı kullanıcının görünür bilgileri).
/// </summary>
public class FriendDto
{
    public int FriendshipId { get; set; }
    public int FriendUserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime Since { get; set; }
}
