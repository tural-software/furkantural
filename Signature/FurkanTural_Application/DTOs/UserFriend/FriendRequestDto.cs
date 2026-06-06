namespace FurkanTural_Application.DTOs.UserFriend;

/// <summary>
/// Bekleyen bir arkadaşlık isteğini temsil eder. Username/DisplayName/AvatarUrl
/// her zaman karşı tarafa aittir (gelen istekte gönderen, giden istekte alıcı).
/// </summary>
public class FriendRequestDto
{
    public int RequestId { get; set; }
    public int RequesterUserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime RequestedAt { get; set; }

    /// <summary>true ise bu istek current kullanıcının GÖNDERDİĞİ (bekleyen) bir istektir (salt-okunur).</summary>
    public bool IsOutgoing { get; set; }
}
