using System.ComponentModel.DataAnnotations;

namespace FurkanTural_Blog.Models;

/// <summary>Bülten sayfasının hem formu hem sonucu. Tek model kullanılır çünkü form JavaScript'siz çalışır: gönderim sayfayı yeniden çizer ve sonuç aynı ekranda, formun bulunduğu yerde görünür.</summary>
public sealed class NewsletterViewModel
{
    [Required(ErrorMessage = "E-posta adresi gerekli.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string? Email { get; set; }

    /// <summary>Tuzak alan. Ekranda ve sekme sırasında yer almaz, dolayısıyla insan doldurmaz; formu körlemesine dolduran bot doldurur.<para>Doğrulama hatası olarak değil sessiz başarı olarak karşılanır: bota hangi alanın onu ele verdiğini söylemek, tuzağı bir sonraki denemede işe yaramaz hâle getirir.</para></summary>
    public string? Website { get; set; }

    public bool Submitted { get; set; }
    public bool Succeeded { get; set; }
    public string? ResultMessage { get; set; }
}
