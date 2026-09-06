using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace FurkanTural_Business.Helpers;

/// <summary>Servislerin iş olaylarını denetim kaydına yazdığı tek kapı. Soyutlaması yoktur; doğrudan somut tip olarak enjekte edilir.<para>Kaynak adı isteğin rotasından türetilir (<see cref="LogSourceBuilder"/>), yani çağrı noktası hiçbir şey söylemese bile satır <c>FurkanTural_API-Blog-Create-Post</c> gibi damgalanır. İş anlamı rotadan ayrılıyorsa label parametresiyle kendi etiketi verilebilir.</para><para>Yazma başarısız olursa istisna yutulur: günlük kaydının kendisi hiçbir işlemi düşürmemelidir. Buna karşılık kayıt sessizce kaybolabilir, dolayısıyla denetim kaydının eksiksizliğine güvenilemez. İstek bağlamı yoksa (arka plan çağrısı, birim testi) IP, yol ve bileşen adı boş geçilir.</para></summary>
public sealed class ActivityLogger(ILogService logService, IHttpContextAccessor httpContextAccessor, IClock clock)
{
    public Task LogAsync(string message, CancellationToken cancellationToken = default)
        => WriteAsync("Information", message, null, cancellationToken);

    public Task LogAsync(string message, string label, CancellationToken cancellationToken = default)
        => WriteAsync("Information", message, label, cancellationToken);

    public Task LogWarningAsync(string message, CancellationToken cancellationToken = default)
        => WriteAsync("Warning", message, null, cancellationToken);

    public Task LogWarningAsync(string message, string label, CancellationToken cancellationToken = default)
        => WriteAsync("Warning", message, label, cancellationToken);

    private async Task WriteAsync(string level, string message, string? label, CancellationToken cancellationToken)
    {
        try
        {
            var context = httpContextAccessor.HttpContext;
            await logService.CreateAsync(new CreateLogDto
            {
                Source = LogSourceBuilder.FromContext(context, LogSources.Api, label),
                Date = clock.UtcNow,
                Level = level,
                Message = message,
                IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
                Path = context?.Request.Path
            }, cancellationToken);
        }
        catch { }
    }
}
