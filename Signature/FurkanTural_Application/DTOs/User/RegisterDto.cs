namespace FurkanTural_Application.DTOs.User;

/// <summary>Kendi kendine kayıt. Girişten farkı, bot doğrulamasının koşulsuz olmasıdır: giriş ucunda Turnstile yalnızca bildirilen uygulama için isteniyorken burada her istekte doğrulanır. AcceptAgreement true gelmezse kayıt reddedilir; kabul edilen sözleşme sürümü de kaydedilir, çünkü sürüm artırıldığında eski onaylar geçersiz sayılır.<para>ConfirmAdult sözleşme onayından ayrı bir iradedir ve o da true gelmeden kayıt açılmaz. İkisi tek kutuya indirilseydi yaş beyanı, okunmadan tıklanan sözleşmenin içinde kaybolurdu. Beyanın anı kullanıcı satırına damgalanır: yaş sınırı ileride değişirse kimin neyi ne zaman beyan ettiği ancak o damgadan okunabilir.</para></summary>
public class RegisterDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public string? TurnstileToken { get; set; }
    public bool AcceptAgreement { get; set; }
    public bool ConfirmAdult { get; set; }
}
