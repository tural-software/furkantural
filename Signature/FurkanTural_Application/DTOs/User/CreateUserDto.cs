namespace FurkanTural_Application.DTOs.User;

public class CreateUserDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int RoleId { get; set; }
    public int? CreatedBy { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}