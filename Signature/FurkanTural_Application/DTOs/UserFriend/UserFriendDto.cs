namespace FurkanTural_Application.DTOs.UserFriend;

public class UserFriendDto
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }
    public int StatusId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
