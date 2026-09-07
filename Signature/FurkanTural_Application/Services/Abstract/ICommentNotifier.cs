namespace FurkanTural_Application.Services.Abstract;

/// <summary>Bekleyen yanıt bildirimlerini gönderir. <see cref="INewsletterDispatcher"/> ile aynı kalıptadır ve aynı gerekçelerle ayrıdır: bir tur gördüğü her bekleyen satırı bir kez dener, aynı satıra o tur içinde geri dönmez ve ilerleme kimlik imleciyle yürür — dolayısıyla geçici bir SMTP arızasında deneme hakları saniyeler içinde tükenmez, denemeler turlar arası beklemeyle kendiliğinden aralanır.<para>Gönderim anında koşullar yeniden okunur. Kuyruğa girdikten sonra alıcı bildirimleri kapatmış, yanıt yayından kaldırılmış ya da üst yorum silinmiş olabilir; bu satırlar başarısız değil <b>atlanmış</b> sayılır, çünkü doğru davranış budur.</para></summary>
public interface ICommentNotifier
{
    /// <summary>Bir tur çalışır ve karara bağlanan satır sayısını döndürür.</summary>
    Task<int> NotifyAsync(CancellationToken cancellationToken = default);
}
