namespace FurkanTural_Application.Services.Abstract;

/// <summary>Mesaj gönderme hız sınırı. TryRegisterSend hem sorar hem sayar: true dönen her çağrı pencereye bir gönderim yazar, bu yüzden yalnızca gönderim gerçekten yapılacakken çağrılmalıdır — kontrol amaçlı çağırmak kotayı tüketir. Sayaçlar bellek içidir ve süreç yeniden başlarsa sıfırlanır.</summary>
public interface IMessageRateLimiter
{
    bool TryRegisterSend(int userId);
}
