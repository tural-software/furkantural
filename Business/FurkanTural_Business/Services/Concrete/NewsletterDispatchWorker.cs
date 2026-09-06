using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Dağıtıcıyı süreç ömrü boyunca çalıştıran döngü. Ayrı bir servis ya da zamanlayıcı kurulamayacağı için tur uygulamanın kendi içindedir; uygulama ayaktaysa dağıtım da ayaktadır.<para>Her tur kendi DI kapsamında koşar: <see cref="INewsletterDispatcher"/> istek ömrüne bağlı bağımlılıklar taşır ve tek bir uzun ömürlü kapsamda tutulursa değişiklik izleyicisi süreç boyunca büyür.</para><para>Turlar arasındaki bekleme yalnızca gecikme değil, aynı zamanda tekrar denemelerin arasındaki nefes payıdır: bir tur bekleyen her satırı bir kez dener, dolayısıyla üç deneme hakkı en erken iki bekleme süresi sonra tükenir. Bu yüzden aralık kısaltılırken hatalı bir adresin ne kadar hızlı yakılacağı da kısalır.</para><para>Beklemeyi erken bitiren tek şey <see cref="NewsletterDispatchSignal"/>'dir; işaret gelmezse tur zaten kendi süresinde uyanır, dolayısıyla işareti kaçırmak yalnızca gecikme demektir.</para><para>Tur içinde çıkan istisna yutulur ve bir sonraki tura geçilir. Dağıtımın yarıda kalması kabul edilebilir, arka plan işinin ölmesi değil: ölürse geri kalan bütün bültenler sessizce sırada bekler.</para></summary>
public sealed class NewsletterDispatchWorker(
    IServiceScopeFactory scopeFactory,
    NewsletterDispatchSignal signal,
    ILogger<NewsletterDispatchWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly NewsletterDispatchSignal _signal = signal;
    private readonly ILogger<NewsletterDispatchWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<INewsletterDispatcher>();
                await dispatcher.DispatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bülten dağıtım turu hata ile bitti; kalan alıcılar bir sonraki turda sürdürülecek.");
            }

            try
            {
                await _signal.WaitAsync(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
