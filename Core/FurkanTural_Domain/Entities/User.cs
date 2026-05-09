using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

public class User : BaseEntity
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }
}
