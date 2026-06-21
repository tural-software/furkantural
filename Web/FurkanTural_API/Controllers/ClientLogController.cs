using Asp.Versioning;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

/// <summary>
/// Kayıtlı ön-yüz uygulamalarının (app-token sahibi) istemci-tarafı hata/uyarı/bilgi
/// loglarını alır ve sistem log tablosuna yazar. Proje adı app_source claim'inden gelir,
/// böylece kaynak (Chat, Portfolio…) güvenilir biçimde damgalanır.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = "AppClient")]
public class ClientLogController(ILogService logService, IClock clock) : BaseApiController
{
    private readonly ILogService _logService = logService;
    private readonly IClock _clock = clock;

    // Trim sınırları DB kolon uzunluklarıyla hizalıdır (LogConfiguration):
    // over-length değer EF SaveChanges'te DbUpdateException → 500 fırlatırdı.
    private const int MaxMessage = 1000;   // nvarchar(1000)
    private const int MaxDetail = 8000;    // nvarchar(max)
    private const int MaxPath = 500;       // nvarchar(500)
    private const int MaxIpAddress = 45;   // nvarchar(45)
    private const int MaxProject = 200;    // nvarchar(200)

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientLogRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return ToActionResult(Result.Fail("Log mesajı boş olamaz.", statusCode: 400));

        var appSource = User.FindFirst("app_source")?.Value;
        if (string.IsNullOrWhiteSpace(appSource))
            return ToActionResult(Result.Fail("Geçersiz uygulama kaynağı.", statusCode: 403));

        var result = await _logService.CreateAsync(new CreateLogDto
        {
            // app_source imzalı claim'den gelir (güvenilir); DB nvarchar(200) için yine de cap'le.
            Project = Trim($"{appSource}_Client", MaxProject),
            Date = _clock.UtcNow,
            Level = NormalizeLevel(request.Level),
            // Önce control-char temizliği (log injection / CRLF satır-sahtelemesi savunması),
            // sonra DB kolon uzunluğuna kırp. Detail çok-satırlı stack trace'tir → \t\r\n korunur.
            Message = Trim(StripControls(request.Message), MaxMessage),
            Detail = Trim(StripControls(request.Detail, keepWhitespace: true), MaxDetail),
            Path = Trim(StripControls(request.Path), MaxPath),
            IpAddress = ResolveIpAddress(request.IpAddress)
        }, cancellationToken);

        // Log yazımı başarısız olsa bile istemciyi meşgul etmeyelim; 204 yeterli.
        return result.Success ? NoContent() : ToActionResult(result);
    }

    // Relay tarayıcının IP'sini gövdede iletir; yalnızca geçerli bir IP ise kabul et,
    // sahte/çöp değerde (log-sahteleme savunması) bağlantı IP'sine düş.
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

    // Control karakterleri (null, CR/LF, escape vb.) kaldır — log injection ve plaintext/SIEM
    // export'ta satır-sahtelemesine karşı savunma. keepWhitespace=true ise \t \r \n korunur
    // (çok-satırlı stack trace okunabilirliği). Kontrol yoksa kopya almaz (hızlı yol).
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
