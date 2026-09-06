namespace FurkanTural_Business.Helpers;

/// <summary>Dağıtıcının bekleme turunu erken bitiren tek yönlü işaret. Soyutlaması yoktur; doğrudan somut tip olarak enjekte edilir.<para>Yalnızca gecikmeyi kısaltır, doğruluğa katkısı yoktur: işaret hiç gelmese de dağıtıcı bir sonraki turunda kuyruğu kendi bulur. Bu yüzden işareti kaçırmak bir hata değil, yalnızca bir dakikaya kadar beklemek demektir — süreçler arası bir kanal olmadığı için yönetici paneli ile dağıtıcı aynı süreçte değilse zaten böyle olur.</para><para>Kapasite birdir. Üst üste iki dağıtım isteği geldiğinde ikinci işaret düşer ve <see cref="SemaphoreFullException"/> yutulur; tek bir uyanma zaten iki kuyruğu da görür.</para></summary>
public sealed class NewsletterDispatchSignal
{
    private readonly SemaphoreSlim _gate = new(0, 1);

    public void Raise()
    {
        try
        {
            _gate.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        => _gate.WaitAsync(timeout, cancellationToken);
}
