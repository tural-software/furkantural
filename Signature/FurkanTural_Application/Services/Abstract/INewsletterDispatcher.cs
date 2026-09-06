namespace FurkanTural_Application.Services.Abstract;

/// <summary>Dağıtıma verilmiş bülten sayılarını sırayla gönderen arka plan işi. Uygulama içinde çalışır; ayrı bir kuyruk servisi ya da zamanlayıcı kurulamayacağı için tur, barındıran sürecin kendi ömrüne bağlıdır.<para>Tek turun sözü şudur: görülen her bekleyen satır bir kez denenir. Turun kendisi bir tur içinde aynı satıra iki kez dönmez, dolayısıyla geçici bir SMTP arızasında deneme hakları saniyeler içinde tükenmez; denemeler turlar arası bekleme kadar aralanır.</para><para>Yeniden başlatmaya dayanıklıdır: kime gönderildiği tek tek kayıtlı olduğu için süreç dağıtımın ortasında düşse bile kimse postayı iki kez almaz ve kalan alıcılar bir sonraki turda sürdürülür.</para><para>Tek örnek varsayılır. Aynı veri tabanına bakan iki süreç aynı anda dağıtım yaparsa satır sahipliği için bir kilit yoktur ve aynı posta iki kez gidebilir.</para></summary>
public interface INewsletterDispatcher
{
    /// <summary>Dağıtıma verilmiş sayılar üzerinde bir tur atar ve denenen dağıtım satırı sayısını döndürür. Sıfır dönmesi yapacak iş kalmadığı anlamına gelir.</summary>
    Task<int> DispatchAsync(CancellationToken cancellationToken = default);
}
