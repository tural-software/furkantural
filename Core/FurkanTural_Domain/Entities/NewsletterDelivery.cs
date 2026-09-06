using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Bir bülten sayısının tek bir aboneye ulaşma girişimi. Satırın varlığı dağıtımı yeniden başlatılabilir kılar: uygulama gönderimin ortasında yeniden başlarsa dağıtıcı kimin aldığını tek tek bilir, dolayısıyla ne baştan başlar ne de sessizce yarıda kalır.<para><see cref="Email"/> gönderim anının kopyasıdır, abonenin bugünkü adresi değil. Bir yıl sonra bakıldığında bültenin gerçekte hangi adrese gittiği sorusunun cevabı burada durur.</para><para><see cref="AttemptCount"/> yalnızca geçici arızalar için vardır ve denemeler dağıtıcının tur aralığıyla kendiliğinden aralanır; ayrı bir bekleme sütunu yoktur. Hak tükendiğinde satır <see cref="Constants.NewsletterDeliveryStatuses.Failed"/> olur ve son hata metni yerinde kalır.</para></summary>
public class NewsletterDelivery : BaseEntity
{
    public int NewsletterIssueId { get; set; }
    public int SubscriberId { get; set; }

    /// <summary>Gönderim anındaki adres kopyası.</summary>
    public string? Email { get; set; }

    public string? Status { get; set; }
    public DateTime? SentAt { get; set; }

    /// <summary>Son denemenin hata metni. Atlanan satırlarda atlanma sebebini taşır.</summary>
    public string? Error { get; set; }

    public int AttemptCount { get; set; }
}
