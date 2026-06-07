namespace FurkanTural_API.Models.Auth;

public class RegisterRequest
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public string? TurnstileToken { get; set; }
    public bool AcceptAgreement { get; set; }
}
