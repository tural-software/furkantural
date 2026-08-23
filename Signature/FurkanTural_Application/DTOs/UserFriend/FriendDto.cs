namespace FurkanTural_Application.DTOs.UserFriend;

/// <summary>Arkadaş listesi satırı; engellenenler listesi de aynı DTO ile döner. IsOnline veri tabanından değil bellekteki bağlantı takibinden gelir, dolayısıyla sunucu yeniden başladığında herkes çevrimdışı görünür; LastSeenAt ise kalıcı kayıttan okunur. Since isteğin yanıtlandığı tarihtir, o boşsa ilişki satırının açıldığı tarihe düşer.</summary>
public class FriendDto
{
    public int FriendshipId { get; set; }
    public int FriendUserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime Since { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
