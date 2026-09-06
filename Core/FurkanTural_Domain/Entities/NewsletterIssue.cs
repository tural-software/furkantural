using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Bir kez yazılıp doğrulanmış adreslere dağıtılan bülten sayısı. Gövde yalnızca yazının kendisidir; başlık, imza ve zorunlu çıkış bağlantısı <see cref="MailTemplate"/> kabuğundan gelir, dolayısıyla çıkış bağlantısını koymayı unutmak mümkün değildir.<para>Alıcı listesi gönderime alındığı anda dondurulur ve her alıcı için bir <see cref="NewsletterDelivery"/> satırı açılır. Sayaçlar bu satırlardan yeniden sayılarak güncellenir, elde tutulan bir toplamdan değil; böylece rakamlar dağıtımın gerçek durumundan ayrışamaz.</para><para>Durum geçişleri için bkz. <see cref="Constants.NewsletterIssueStatuses"/>. Sayıyı pasife almak ya da silmek dağıtımı durdurur: küresel süzgeç kaydı dağıtıcının görüş alanından çıkarır.</para></summary>
public class NewsletterIssue : BaseEntity
{
    public string? Subject { get; set; }

    /// <summary>Yazının HTML gövdesi. Şablonun <c>{{Body}}</c> yer tutucusuna olduğu gibi, kaçışsız yerleştirilir.</summary>
    public string? Body { get; set; }

    public string? Status { get; set; }

    /// <summary>Alıcı listesinin dondurulduğu an. Bu andan sonra abone olan kimse bu sayıyı almaz.</summary>
    public DateTime? QueuedAt { get; set; }

    /// <summary>Her alıcı için karara varıldığı an.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Dondurma anındaki alıcı sayısı; sonradan değişmez ve payda olarak kullanılır.</summary>
    public int RecipientCount { get; set; }

    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
}
