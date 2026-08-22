using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Http;

namespace FurkanTural_Business.Helpers;

/// <summary>
/// Servislerin iş olaylarını denetim kaydına yazdığı tek kapı. Soyutlaması yoktur; doğrudan somut
/// tip olarak enjekte edilir.
///
/// Yazma başarısız olursa istisna yutulur: günlük kaydının kendisi hiçbir işlemi düşürmemelidir. Buna
/// karşılık kayıt sessizce kaybolabilir, dolayısıyla denetim kaydının eksiksizliğine güvenilemez.
/// İstek bağlamı yoksa (arka plan çağrısı, birim testi) IP ve yol boş geçilir.
/// </summary>
public sealed class ActivityLogger(ILogService logService, IHttpContextAccessor httpContextAccessor, IClock clock)
{
    public async Task LogAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await logService.CreateAsync(new CreateLogDto
            {
                Project = "FurkanTural_API",
                Date = clock.UtcNow,
                Level = "Information",
                Message = message,
                IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                Path = httpContextAccessor.HttpContext?.Request.Path
            }, cancellationToken);
        }
        catch { }
    }
}