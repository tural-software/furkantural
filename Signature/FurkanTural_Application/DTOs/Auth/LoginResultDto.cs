namespace FurkanTural_Application.DTOs.Auth;

public class LoginResultDto
{
    public string? Token { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? RoleName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
}
