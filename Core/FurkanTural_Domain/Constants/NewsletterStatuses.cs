namespace FurkanTural_Domain.Constants;

/// <summary>Bir bülten sayısının yaşam çizgisi. Geçişler tek yönlüdür: taslak yazılır, gönderime alınır, biter. Geri dönüş yoktur — gönderilmiş bir postayı geri almak mümkün olmadığı için durumu geri almak da yalnızca kaydı yalancı hâle getirirdi.<para>Gönderimin duraklatılması ayrı bir durum değildir: sayıyı pasife almak ya da silmek küresel süzgeç yüzünden dağıtıcının görüş alanından çıkarır, dolayısıyla mevcut aktiflik anahtarı aynı zamanda acil durdurmadır. Geri açıldığında kaldığı yerden sürer, çünkü kime gönderildiği tek tek kayıtlıdır.</para></summary>
public static class NewsletterIssueStatuses
{
    /// <summary>Yazılıyor. Konu ve gövde serbestçe değiştirilebilir, hiçbir alıcı kaydı yoktur.</summary>
    public const string Draft = "Draft";

    /// <summary>Alıcı listesi dondurulmuş ve dağıtım sürüyor. Bu noktadan sonra konu ve gövde değiştirilemez: bir bölümü eski metni, bir bölümü yenisini alırdı.</summary>
    public const string Sending = "Sending";

    /// <summary>Her alıcı için karar verilmiş — gönderildi, kalıcı olarak başarısız oldu ya da atlandı. Başarısızlık bulunması durumu değiştirmez; sayılar ne olduğunu zaten söyler.</summary>
    public const string Sent = "Sent";

    public const int MaxLength = 20;
}

/// <summary>Tek bir alıcının o sayıdaki durumu. Dağıtıcı yalnızca <see cref="Pending"/> satırlara dokunur, dolayısıyla süreç ortasında yeniden başlatılan bir uygulama kaldığı yerden sürer ve kimse postayı iki kez almaz.</summary>
public static class NewsletterDeliveryStatuses
{
    /// <summary>Sıraya alındı, henüz denenmedi ya da denendi de geçici olarak başarısız oldu.</summary>
    public const string Pending = "Pending";

    /// <summary>SMTP teslim aldı. Bu, postanın gelen kutusuna düştüğünü değil, sunucunun kabul ettiğini söyler.</summary>
    public const string Sent = "Sent";

    /// <summary>Deneme hakkı tükendi. Son hata metni satırda durur.</summary>
    public const string Failed = "Failed";

    /// <summary>Liste dondurulduktan sonra abonelikten çıkıldığı için gönderilmedi. Başarısızlık değildir: doğru davranış budur.</summary>
    public const string Skipped = "Skipped";

    public const int MaxLength = 20;
}
