namespace FurkanTural_Admin.Models.Auth;

public class LoginResultModel
{
    public string? Token { get; set; }
    public string? Username { get; set; }
    public DateTime ExpiresAt { get; set; }
}
