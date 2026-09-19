namespace FurkanTural_API.Models.Subscriber;

/// <summary>Bülten akışının tek gövde tipi. Üç alan da opsiyoneldir çünkü her uç yalnızca kendi ihtiyacını okur: abonelik ve çıkış isteği <c>Email</c> ile <c>TurnstileToken</c>'ı, onay ve çıkışın kendisi yalnızca <c>Token</c>'ı.<para><c>Token</c> bir kimlik bilgisidir ve yalnızca adrese giden postanın bağlantısında bulunur. Çıkış ucu adres kabul etmez: adres tek başına yeterli olsaydı herhangi biri başkasının aboneliğini iptal edebilirdi.</para></summary>
public class SubscribeRequest
{
    public string? Email { get; set; }

    /// <summary>Cloudflare Turnstile jetonu. Doğrulama koşulsuzdur; iletişim formuyla aynı gerekçeyle uygulama listesine bakan koşullu model bu akışta doğrulamayı sessizce hiç çalıştırmazdı.</summary>
    public string? TurnstileToken { get; set; }

    /// <summary>Postayla gönderilen tek kullanımlık doğrulama jetonu.</summary>
    public string? Token { get; set; }
}
