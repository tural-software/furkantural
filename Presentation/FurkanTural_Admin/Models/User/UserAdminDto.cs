namespace FurkanTural_Admin.Models.User;

public sealed class UserAdminDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
