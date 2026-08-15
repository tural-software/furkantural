namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Arama başlatma hız sınırı. TryStartCall hem sorar hem sayar: true dönen her çağrı pencereye bir
/// deneme yazar, bu yüzden yalnızca arama gerçekten başlatılacakken çağrılmalıdır — kontrol amaçlı
/// çağırmak kotayı tüketir. Sayaçlar bellek içidir ve süreç yeniden başlarsa sıfırlanır.
/// </summary>
public interface ICallRateLimiter
{
    bool TryStartCall(int userId);
}