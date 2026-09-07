using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Bir yazının altına bırakılan yorum. Üyelik istenmez: <see cref="AuthorName"/> ile <see cref="AuthorEmail"/> yazanın kendi beyanıdır ve doğrulanmaz, dolayısıyla bir kimlik değil yalnızca bir imzadır.<para>Adres hiçbir yerde yayımlanmaz. İki işi vardır: aynı kişiyi turdan tura tanımak ve <see cref="NotifyOnReply"/> açıksa yoruma gelen yanıtı haber vermek. Gövdede saklanması bu ikinci iş yüzünden zorunludur — bildirim gönderilecek adres yorumun kendisinde durmazsa onu başka hiçbir yerden bulamayız.</para><para>Yayına girmek <see cref="Status"/>'e bağlıdır, aktifliğe değil. Ayrım bilinçli: aktiflik yöneticinin genel anahtarıdır ve her modülde aynı şeyi yapar, durum ise yorumun kendi denetim çizgisidir. Bir yorum onaylanmış ama pasife alınmış olabilir; o zaman da görünmez, çünkü okumalar iki koşulu birlikte arar.</para><para><see cref="ParentId"/> derinlik sınırı tanımaz: yanıtın yanıtı da, onun yanıtı da açılabilir. Zincirin uzunluğu bir konuşmanın kaç el gidip geldiğine bağlıdır ve bunu veri tabanında kesmek, sohbeti veriyle değil şemayla susturmak olurdu. Dar ekranın sınırı ise gerçektir ama sunumdadır: girinti belli bir seviyeden sonra durur, yanıtın kime verildiği ok ile yazılır.</para></summary>
public class Comment : BaseEntity
{
    public int BlogId { get; set; }

    /// <summary>Yanıt verilen yorum. Boşsa yorum doğrudan yazıya bırakılmıştır. Dolu bir değer kendisi de bir yanıt olabilir; zincir kaç halka sürerse sürsün geçerlidir.</summary>
    public int? ParentId { get; set; }

    public string? AuthorName { get; set; }

    /// <summary>Yazanın beyan ettiği adres. <b>Hiçbir okumada dışarı verilmez</b> — yorum listesi bu alanı taşımayan bir DTO döndürür, dolayısıyla sayfayı çeken biri yorumcuların adreslerini toplayamaz.</summary>
    public string? AuthorEmail { get; set; }

    public string? Body { get; set; }

    /// <summary>Denetim durumu. Yeni yorum daima beklemede açılır: onaydan önce görünen bir yorum, spam'i okurun karşısına koyup temizliği yetişme hızına bağlardı.</summary>
    public string? Status { get; set; }

    /// <summary>Onaylandığı an. Durumdan türetilebilir görünür ama türetilemez — reddedilip yeniden onaylanan bir yorumun ilk yayın anı yalnızca burada durur.</summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>Bu yoruma yanıt geldiğinde adrese posta gidip gitmeyeceği. Varsayılan kapalıdır ve formda açıkça işaretlenir; istenmemiş posta göndermemenin tek güvencesi budur.<para>Çıkış bağlantısı bunu adres bazında kapatır, yorum bazında değil: "bana posta göndermeyi bırak" diyen biri yalnızca o tek yorum için söylemiyordur.</para></summary>
    public bool NotifyOnReply { get; set; }
}
