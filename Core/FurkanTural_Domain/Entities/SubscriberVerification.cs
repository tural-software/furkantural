using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Bir <see cref="Subscriber"/> adresinin sahibi olduğunu kanıtlamak için üretilen tek kullanımlık doğrulama isteğinin kaydı. Jetonun kendisi burada durmaz: e-postaya giden düz değer bir kimlik bilgisidir, tabloya yalnızca türevi yazılır.<para>İki amacı vardır ve <see cref="Purpose"/> hangisi olduğunu söyler (bkz. <see cref="Constants.SubscriberVerificationPurposes"/>). Abonelik açılışı ile çıkış aynı mekanizmayı paylaşır, çünkü ikisinin de tek sorusu aynıdır: bu isteği adresin sahibi mi yaptı?</para><para>Tüketilen satır silinmez, ConsumedAt damgalanır. Kayıtlar bilerek kalıcıdır; bir adresin listeye ne zaman ve nereden girdiğinin izi, izinli pazarlamanın kendisini kanıtlayan şeydir.</para></summary>
public class SubscriberVerification : BaseEntity, ISingleUseToken
{
    public int SubscriberId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string? RequestIpAddress { get; set; }
    public string? RequestUserAgent { get; set; }
}
