using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class User : BaseEntity
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }

    // Chat / üyelik alanları
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}
