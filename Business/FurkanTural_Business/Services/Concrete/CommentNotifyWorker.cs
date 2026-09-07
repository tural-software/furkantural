using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Yanıt bildirimi turunu süreç ömrü boyunca çalıştıran döngü; yapısı <see cref="NewsletterDispatchWorker"/> ile birebir aynıdır ve aynı gerekçelerle öyledir — ayrı bir servis kurulamayacağı için tur uygulamanın içindedir, her tur kendi DI kapsamında koşar ve istisna yutulur çünkü arka plan işinin ölmesi, bir turun yarıda kalmasından pahalıdır.<para>Aralık bültenden kısadır. İki iş farklı şeyler bekletiyor: bülten binlerce adresi sıraya alır ve bir dakikalık nefes payı denemeleri aralamaya yarar; yorum bildirimi tek kişiliktir ve yanıtını bekleyen birini gereksiz yere bekletmemek gerekir. İşaret zaten turu hemen uyandırır, aralık yalnızca işaretin kaçırıldığı hâlin üst sınırıdır.</para></summary>
public sealed class CommentNotifyWorker(
    IServiceScopeFactory scopeFactory,
    CommentNotifySignal signal,
    ILogger<CommentNotifyWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly CommentNotifySignal _signal = signal;
    private readonly ILogger<CommentNotifyWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notifier = scope.ServiceProvider.GetRequiredService<ICommentNotifier>();
                await notifier.NotifyAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yorum bildirimi turu hata ile bitti; bekleyen bildirimler bir sonraki turda sürdürülecek.");
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
