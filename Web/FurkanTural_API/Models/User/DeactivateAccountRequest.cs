namespace FurkanTural_API.Models.User;

/// <summary>Kullanıcının kendi hesabını kapatma isteği. Parola oturumun yanında ayrıca istenir: hesabı kapatmak kullanıcıyı her yerden düşürür ve geri açmak posta kutusuna erişim ister, dolayısıyla açık bırakılmış bir makineden yapılabilmemesi gerekir.</summary>
public class DeactivateAccountRequest
{
    public string? Password { get; set; }
}
