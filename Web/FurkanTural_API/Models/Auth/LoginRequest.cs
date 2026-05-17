namespace FurkanTural_API.Models.Auth;

public class LoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? AppSource { get; set; }
}