using System.ComponentModel.DataAnnotations;

namespace FurkanTural_Chat.Models.Auth;

public class LoginRequestModel
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Şifre gereklidir.")]
    public string? Password { get; set; }

    public string? TurnstileToken { get; set; }
}
