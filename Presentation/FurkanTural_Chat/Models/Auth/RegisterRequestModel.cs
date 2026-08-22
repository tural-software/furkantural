using System.ComponentModel.DataAnnotations;

namespace FurkanTural_Chat.Models.Auth;

public class RegisterRequestModel
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-100 karakter olmalı.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "E-posta gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string? Email { get; set; }

    [Display(Name = "Görünen ad")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [MinLength(4, ErrorMessage = "Şifre en az 4 karakter olmalı.")]
    public string? Password { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Üyelik sözleşmesini ve Gizlilik Politikasını kabul etmelisiniz.")]
    public bool AcceptAgreement { get; set; }

    public string? TurnstileToken { get; set; }
}