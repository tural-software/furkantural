using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Bültenin çift onaylı abonelik yaşam döngüsü. <see cref="ISubscriberService"/>'ten ayrıdır: o yönetim panelinin CRUD'u, bu ziyaretçinin akışıdır ve tek sorusu vardır — bu isteği adresin sahibi mi yaptı?<para>Jeton kriptografik rastgeleden üretilir, tabloya SHA-256 türevi yazılır; veri tabanını okuyabilen biri jetonu geri üretemez. Düz hâli servisten hiç çıkmaz, yalnızca giden postanın bağlantısında bulunur.</para><para><b>Hiçbir uç bir adresin listede olup olmadığını ele vermez.</b> <see cref="SubscribeAsync"/> ile <see cref="RequestUnsubscribeAsync"/> adres kayıtlı olsa da olmasa da aynı metni ve aynı durumu döndürür; aksi hâlde uçlar, kimin abone olduğunu tek tek sınayabilen bir sorgulama aracına dönerdi.</para><para>Bu güvencenin bilinen tek sınırı posta sunucusunun arızalı olduğu andır: posta göndermesi gereken dal başarısızlığı yukarı taşır, göndermeyen dal ise başarıyla döner, dolayısıyla arıza sırasında iki durum hata kanalından ayırt edilebilir. Arızayı yutmak yerine bildirmek bilinçli bir tercihtir — gönderilemeyen bir bağlantıyı başarı diye sunmak, kullanıcıyı hiç ulaşmayacak bir postayı beklemeye bırakırdı (aynı gerekçe için bkz. <see cref="IAccountActivationService"/>). Sızıntının değeri düşüktür: sınanacak adresi zaten bilmek gerekir ve pencere yalnızca arıza süresince açıktır.</para><para>Çıkış da doğrulama ister. Adres tek başına yeterli olsaydı herhangi biri başkasının aboneliğini iptal edebilirdi; bu yüzden <see cref="RequestUnsubscribeAsync"/> yalnızca bağlantı gönderir, listeden düşüren <see cref="UnsubscribeAsync"/> ise yalnızca jeton kabul eder.</para></summary>
public interface INewsletterService
{
    /// <summary>Adresi doğrulanmamış olarak kaydeder ve doğrulama bağlantısını gönderir. Bot doğrulaması koşulsuzdur: iletişim formuyla aynı gerekçeyle, uygulama listesine bakan koşullu model bu akışta doğrulamayı sessizce hiç çalıştırmazdı.</summary>
    Task<Result> SubscribeAsync(string? email, string? turnstileToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    /// <summary>Doğrulama jetonunu harcar ve aboneliği açar. Jeton tek kullanımlıktır; süresi geçmiş ile harcanmış ayrı ayrı bildirilir, çünkü bağlantıyı elinde tutan kişi zaten meşru kabul edilir ve ayırmamak yalnızca onu ne yapacağını bilmez hâlde bırakırdı.</summary>
    Task<Result> ConfirmAsync(string? token, CancellationToken cancellationToken = default);

    /// <summary>Çıkış bağlantısını adrese gönderir. Listeden düşürmez — yalnızca adresin sahibine ulaşır.</summary>
    Task<Result> RequestUnsubscribeAsync(string? email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    /// <summary>Çıkış jetonunu harcar ve aboneliği listeden düşürür.</summary>
    Task<Result> UnsubscribeAsync(string? token, CancellationToken cancellationToken = default);
}
