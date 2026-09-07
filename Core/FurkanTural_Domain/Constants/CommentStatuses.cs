namespace FurkanTural_Domain.Constants;

/// <summary>Bir yorumun denetim çizgisi. Geçişler tek yönlü değildir: reddedilmiş bir yorum yeniden onaylanabilir, çünkü red bir cezalandırma değil bir karardır ve karar değişebilir.<para>Aktiflikten ayrıdır. Aktiflik yöneticinin her modülde aynı işi yapan genel anahtarıdır; durum yorumun kendi denetimidir. Okurun gördüğü satır ikisini birden geçmiş olandır, dolayısıyla pasife alınan onaylı bir yorum da görünmez.</para></summary>
public static class CommentStatuses
{
    /// <summary>Bırakıldı, henüz karara bağlanmadı. Yazana görünür — kendi yorumunu bekliyor olarak görmek, gönderdiğinin kaybolmadığını bilmenin tek yoludur — ama başkasına görünmez.</summary>
    public const string Pending = "Pending";

    /// <summary>Yayında. Yalnızca bu durumdaki satırlar okura çizilir ve yanıt alabilir.</summary>
    public const string Approved = "Approved";

    /// <summary>Yayımlanmayacak. Satır silinmez: neyin reddedildiğini görmeden aynı gönderenin ikinci denemesini tanımak mümkün olmaz.</summary>
    public const string Rejected = "Rejected";

    public const int MaxLength = 20;

    public static bool IsKnown(string? value)
        => value is Pending or Approved or Rejected;
}

/// <summary>Tek bir yanıt bildiriminin durumu. Dağıtıcı yalnızca <see cref="Pending"/> satırlara dokunur; süreç ortasında yeniden başlatılan bir uygulama kaldığı yerden sürer ve kimse aynı bildirimi iki kez almaz.</summary>
public static class CommentNotificationStatuses
{
    /// <summary>Kuyrukta. Henüz denenmedi ya da denendi de geçici olarak başarısız oldu.</summary>
    public const string Pending = "Pending";

    /// <summary>SMTP teslim aldı.</summary>
    public const string Sent = "Sent";

    /// <summary>Deneme hakkı tükendi. Son hata metni satırda durur.</summary>
    public const string Failed = "Failed";

    /// <summary>Kuyruğa girdikten sonra gönderilmemesi gerektiği anlaşıldı: alıcı bildirimleri kapatmış, yanıt yayından kaldırılmış ya da üst yorum silinmiş olabilir. Başarısızlık değildir, doğru davranış budur.</summary>
    public const string Skipped = "Skipped";

    public const int MaxLength = 20;
}
