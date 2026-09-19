namespace FurkanTural_Blog.Services;

/// <summary>Bülten uçlarına giden çağrılar. Hepsi API'nin kendi metnini olduğu gibi taşır: ne olduğunu ziyaretçiye anlatan cümle API'de yazılıdır ve buradaki kopyası onunla ayrışmamalıdır.<para>Abonelik ve çıkış istekleri, adresin listede olup olmadığını bilerek ele vermez; bu yüzden ikisinin de "bulunamadı" hâli yoktur.</para></summary>
public interface INewsletterClient
{
    Task<NewsletterOutcome> SubscribeAsync(string email, string? turnstileToken, CancellationToken ct = default);

    /// <summary>Doğrulama jetonunu harcar ve aboneliği açar.</summary>
    Task<NewsletterOutcome> ConfirmAsync(string token, CancellationToken ct = default);

    /// <summary>Çıkış bağlantısının adrese gönderilmesini ister. Listeden düşürmez.</summary>
    Task<NewsletterOutcome> RequestUnsubscribeAsync(string email, string? turnstileToken, CancellationToken ct = default);

    /// <summary>Çıkış jetonunu harcar ve aboneliği listeden düşürür.</summary>
    Task<NewsletterOutcome> UnsubscribeAsync(string token, CancellationToken ct = default);
}

/// <summary>Sonuç ve ziyaretçiye gösterilecek metin. Metin boş dönerse çağıran kendi yedeğini kullanır.</summary>
public readonly record struct NewsletterOutcome(bool Succeeded, string? Message);
