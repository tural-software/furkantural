using System.ComponentModel.DataAnnotations;

namespace FurkanTural_Chat.Models.Auth;

/// <summary>Kullanıcının kendi hesabını kapatma formu. Oturum açıkken bile parola istenir: kapatma kullanıcıyı her yerden düşürür ve geri açmak posta kutusuna erişim ister, dolayısıyla başında kimse olmayan bir makineden yapılabilmemesi gerekir.</summary>
public class CloseAccountModel
{
    [Required(ErrorMessage = "Parolanızı girmeniz gerekiyor.")]
    public string? Password { get; set; }

    public string? Error { get; set; }
}
