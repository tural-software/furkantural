namespace FurkanTural_Blog.Services;

/// <summary>Abonelik ucuna giden tek çağrı. API'nin kendi metnini olduğu gibi taşır: "zaten abone listesinde" gibi durumlar hata değil bilgidir ve ziyaretçinin okuması gereken metin API'de yazılıdır.</summary>
public interface INewsletterClient
{
    Task<NewsletterOutcome> SubscribeAsync(string email, CancellationToken ct = default);
}

/// <summary>Sonuç ve ziyaretçiye gösterilecek metin. Metin boş dönerse çağıran kendi yedeğini kullanır.</summary>
public readonly record struct NewsletterOutcome(bool Succeeded, string? Message);
