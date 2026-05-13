namespace FurkanTural_Admin.Models.User;

public sealed class UserFormDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }
}
