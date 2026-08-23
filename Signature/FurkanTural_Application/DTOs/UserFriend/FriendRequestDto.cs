namespace FurkanTural_Application.DTOs.UserFriend;

/// <summary>Bekleyen arkadaşlık isteği; gelen ve giden istekler aynı DTO ile döner, ayrımı IsOutgoing yapar. Dikkat: Username/DisplayName/AvatarUrl her zaman karşı tarafı anlatır, RequesterUserId ise her zaman isteği başlatanı gösterir. Giden istekte bu ikisi farklı kişilerdir — profil alanları alıcıya, RequesterUserId çağıranın kendisine aittir.</summary>
public class FriendRequestDto
{
    public int RequestId { get; set; }
    public int RequesterUserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime RequestedAt { get; set; }
    public bool IsOutgoing { get; set; }
}
