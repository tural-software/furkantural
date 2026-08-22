namespace FurkanTural_Admin.Models.UserFriend;

public sealed class UserFriendAdminDto
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }

    /// <summary>Silinmiş kullanıcıda null gelir; yönetim listesi bu satırları da gösterir.</summary>
    public string? RequesterUsername { get; set; }
    public string? AddresseeUsername { get; set; }

    public int StatusId { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusName { get; set; }
    public DateTime? RespondedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}