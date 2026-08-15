namespace FurkanTural_Application.DTOs.User;

public class UserDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public int RoleId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}