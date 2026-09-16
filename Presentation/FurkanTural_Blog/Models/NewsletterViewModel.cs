using System.ComponentModel.DataAnnotations;

namespace FurkanTural_Blog.Models;

/// <summary>Bülten sayfasının hem formu hem sonucu. Tek model kullanılır çünkü gönderim sayfayı yeniden çizer ve sonuç aynı ekranda, formun bulunduğu yerde görünür.</summary>
public sealed class NewsletterViewModel
{
    [Required(ErrorMessage = "E-posta adresi gerekli.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string? Email { get; set; }

    /// <summary>Tuzak alan. Ekranda ve sekme sırasında yer almaz, dolayısıyla insan doldurmaz; formu körlemesine dolduran bot doldurur.<para>Doğrulama hatası olarak değil sessiz başarı olarak karşılanır: bota hangi alanın onu ele verdiğini söylemek, tuzağı bir sonraki denemede işe yaramaz hâle getirir.</para><para>Turnstile geldikten sonra da durur; ikisi farklı botları eler ve tuzak alanın maliyeti sıfırdır.</para></summary>
    public string? Website { get; set; }

    /// <summary>Cloudflare Turnstile jetonu. Asıl doğrulamayı API yapar; buradaki erken ret onun yerine geçmez, yalnızca jeton hiç gönderilmemiş istekleri ağa çıkmadan eler.</summary>
    public string? TurnstileToken { get; set; }

    public bool Submitted { get; set; }
    public bool Succeeded { get; set; }
    public string? ResultMessage { get; set; }
}

/// <summary>Doğrulama ve çıkış bağlantılarının indiği sayfa. Jeton adres satırından gelir ve tek kullanımlıktır. Sayfa önce onay düğmesini (<see cref="PendingAction"/>), düğmeye basıldıktan sonra sonucu gösterir.</summary>
public sealed class NewsletterTokenViewModel
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }

    /// <summary>Jeton hiç gelmediğinde sayfa sonuç değil yönerge gösterir: bağlantısız gelen ziyaretçiye "geçersiz" demek, yanlış bir şey yaptığını sanmasına yol açar.</summary>
    public bool TokenMissing { get; set; }

    public const string ConfirmAction = "confirm";

    public const string UnsubscribeAction = "unsubscribe";

    public string? PendingAction { get; set; }

    public string? Token { get; set; }
}
