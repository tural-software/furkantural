using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// İki kullanıcı arasındaki arkadaşlık ilişkisi; RequesterId ve AddresseeId <see cref="User"/>'a,
/// StatusId ise <see cref="Status"/> tablosunun Friendship grubuna bakar
/// (<see cref="Constants.StatusDefinitions.FriendshipCodes"/>). BlockedByUserId yalnızca Blocked
/// durumunda dolar ve engeli kaldırabilecek tek kullanıcıyı gösterir.
/// </summary>
public class UserFriend : BaseEntity
{
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }
    public int StatusId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int? BlockedByUserId { get; set; }
}
