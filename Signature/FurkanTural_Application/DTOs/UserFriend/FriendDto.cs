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

    /// <summary>Arkadaş şu an çevrimiçi mi (en az bir aktif SignalR bağlantısı var mı)?</summary>
    public bool IsOnline { get; set; }

    /// <summary>Çevrimdışıysa en son görüldüğü an (UTC); hiç bağlanmadıysa null.</summary>
    public DateTime? LastSeenAt { get; set; }
}
