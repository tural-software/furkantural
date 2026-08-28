using System.ComponentModel.DataAnnotations;

namespace FurkanTural_Chat.Models.Auth;

public class RegisterRequestModel
{
    public const string PasswordPattern =
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!#$%()*+,\-./:;=?@\[\]^_{|}~])[A-Za-z\d!#$%()*+,\-./:;=?@\[\]^_{|}~]{6,64}$";

    [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-100 karakter olmalı.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "E-posta gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string? Email { get; set; }

    [Display(Name = "Görünen ad")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [RegularExpression(PasswordPattern, ErrorMessage =
        "Parola 6-64 karakter olmalı; bir büyük harf, bir küçük harf, bir rakam ve bir sembol içermeli.")]
    public string? Password { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Üyelik sözleşmesini ve Gizlilik Politikasını kabul etmelisiniz.")]
    public bool AcceptAgreement { get; set; }

    public string? TurnstileToken { get; set; }
}
