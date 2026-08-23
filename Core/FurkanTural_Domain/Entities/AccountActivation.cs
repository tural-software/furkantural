using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Pasif bir <see cref="User"/>'ı yeniden aktifleştirmek için üretilen tek kullanımlık doğrulama isteğinin kaydı. Jetonun kendisi burada durmaz: e-postaya giden düz değer bir kimlik bilgisidir, tabloya yalnızca türevi yazılır, dolayısıyla veri tabanını okuyabilen biri jetonu geri üretemez.<para>Tüketilen satır silinmez, ConsumedAt damgalanır ve dolu ConsumedAt taşıyan satır bir daha kullanılamaz. Kayıtlar bilerek kalıcıdır — aktivasyon girişimlerinin izlenebilir bir izi kalsın diyedir, süresi geçmişleri toplayan bir temizlik işi yoktur.</para><para>RequestIpAddress gerçek ziyaretçi adresini taşımalıdır: Connection.RemoteIpAddress Cloudflare kenar adresini verir, değer UseRealClientIp middleware'inden sonra okunmazsa bütün satırlar aynı kenar adresini taşır ve iz anlamsızlaşır. Trigger sabit bir listeye bağlı değildir, <see cref="Log"/>'daki Level gibi serbest metindir; akış "Login" ve "Register" yazar.</para></summary>
public class AccountActivation : BaseEntity
{
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string? RequestIpAddress { get; set; }
    public string? RequestUserAgent { get; set; }
    public string? Trigger { get; set; }
}
