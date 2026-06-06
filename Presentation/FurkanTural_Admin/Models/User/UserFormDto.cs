namespace FurkanTural_Admin.Models.User;

public sealed class UserFormDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
}
