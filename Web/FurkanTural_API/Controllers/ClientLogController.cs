using Asp.Versioning;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Log;
using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

/// <summary>Kayıtlı ön-yüz uygulamalarının (app-token sahibi) istemci-tarafı hata/uyarı/bilgi loglarını alır ve sistem log tablosuna yazar. Kaynağın uygulama parçası app_source claim'inden gelir, gövdeden değil: tarayıcı kendi kaydını başka bir uygulamanın üstüne yazamasın diye. Gövdeden yalnızca bileşen ve işlem parçaları alınır ve <see cref="FurkanTural_Domain.Constants.LogSources"/> bunları temizler.<para>Aşağıdaki kırpma sınırları günlük tablosunun kolon genişliklerini yansıtır. Hizasız kalırlarsa uzun bir değer kaydetme anında hata doğurur ve istemcinin gönderdiği günlük, uygulamanın kendi 500'üne dönüşür.</para><para>Yazma başarısız olsa da istemciye 204 döner: tarayıcı kendi hatasını bildirmeye çalışırken ikinci bir hatayla oyalanmamalıdır.</para></summary>
[ApiVersion("1.0")]
[Authorize(Policy = "AppClient")]
public class ClientLogController(ILogService logService, IClock clock) : BaseApiController
{
    private readonly ILogService _logService = logService;
    private readonly IClock _clock = clock;

    private const int MaxMessage = 1000;
    private const int MaxDetail = 8000;
    private const int MaxPath = 500;
    private const int MaxIpAddress = 45;
    private const int MaxSource = 200;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientLogRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return ToActionResult(Result.Fail("Log mesajı boş olamaz.", statusCode: 400));

        var app = LogSources.ForApp(User.FindFirst("app_source")?.Value);
        if (app is null)
            return ToActionResult(Result.Fail("Geçersiz uygulama kaynağı.", statusCode: 403));

        var result = await _logService.CreateAsync(new CreateLogDto
        {
            Source = Trim(LogSources.Compose(app, request.Component), MaxSource),
            Date = _clock.UtcNow,
            Level = NormalizeLevel(request.Level),
            Message = Trim(StripControls(request.Message), MaxMessage),
            Detail = Trim(StripControls(request.Detail, keepWhitespace: true), MaxDetail),
            Path = Trim(StripControls(request.Path), MaxPath),
            IpAddress = ResolveIpAddress(request.IpAddress)
        }, cancellationToken);

        return result.Success ? NoContent() : ToActionResult(result);
    }

    /// <summary>Ziyaretçinin IP'si gövdede taşınır, çünkü isteği tarayıcı değil aradaki sunum projesi iletir ve bağlantı IP'si onu gösterir. Gövdeden gelen değer yalnızca gerçekten ayrıştırılabiliyorsa kabul edilir; aksi hâlde bağlantı IP'sine düşülür. Bu eleme olmasa günlük tablosuna istenen her şey IP diye yazdırılabilirdi.</summary>
    private string? ResolveIpAddress(string? candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            var trimmed = candidate.Trim();
            if (trimmed.Length <= MaxIpAddress && System.Net.IPAddress.TryParse(trimmed, out _))
                return trimmed;
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>Kontrol karakterleri temizlenir. Satır sonu taşıyan bir mesaj düz metin günlükte veya bir SIEM aktarımında sahte satır üretebilir, yani istemci kendi kaydının yanına uydurma kayıtlar ekleyebilirdi. keepWhitespace verildiğinde sekme ve satır sonu korunur; yığın izleri okunabilir kalsın diye yalnızca ayrıntı alanında kullanılır.<para>Temizlenecek bir şey yoksa dize kopyalanmaz, olduğu gibi geri döner.</para></summary>
    private static string? StripControls(string? value, bool keepWhitespace = false)
    {
        if (string.IsNullOrEmpty(value)) return value;
        static bool Disallowed(char c, bool keepWs)
            => char.IsControl(c) && !(keepWs && (c == '\t' || c == '\n' || c == '\r'));
        if (!value.Any(c => Disallowed(c, keepWhitespace))) return value;
        return new string(value.Where(c => !Disallowed(c, keepWhitespace)).ToArray());
    }

    private static string NormalizeLevel(string? level) => (level ?? "").Trim().ToLowerInvariant() switch
    {
        "error" or "err" or "fatal" or "critical" => "Error",
        "warn" or "warning" => "Warning",
        _ => "Information"
    };

    private static string? Trim(string? value, int max)
        => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}
