namespace FurkanTural_Application.DTOs.UserFriend;

/// <summary>Arkadaşlık ilişkisinin ham hâli. Bir kullanıcı çifti için tek satır tutulur ve durum değiştikçe aynı satır güncellenir; engellemede engelleyen taraf Requester'a taşındığı için RequesterId ilişkiyi ilk başlatan kişi olmayabilir. StatusId, statü tablosuna bakan bir anahtardır ve sabit değildir — kod tarafında statüler daima Group ve Code ikilisiyle çözülür.</summary>
public class UserFriendDto
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }
    public int StatusId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
