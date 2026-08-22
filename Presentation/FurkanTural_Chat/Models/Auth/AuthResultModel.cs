namespace FurkanTural_Chat.Models.Auth;

public class AuthResultModel
{
    public string? Token { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? RoleName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool MembershipAgreementAccepted { get; set; }
}