namespace FurkanTural_Application.DTOs.User;

public class RegisterDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public string? TurnstileToken { get; set; }

    /// <summary>Kullanıcı üyelik sözleşmesini kabul etti mi? Kayıt için zorunludur.</summary>
    public bool AcceptAgreement { get; set; }
}
