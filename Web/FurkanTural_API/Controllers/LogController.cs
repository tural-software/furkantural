using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiVersion("1.0")]
public class LogController(ILogService logService, IClock clock) : BaseApiController
{
    private readonly ILogService _logService = logService;
    private readonly IClock _clock = clock;

    private const int MaxMessage = 1000;
    private const int MaxDetail = 8000;
    private const int MaxPath = 500;
    private const int MaxProject = 200;

    /// <summary>Sistem logunu ID ile getir</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _logService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm sistem loglarını listele</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _logService.GetAllAsync(cancellationToken));

    /// <summary>Sistem loglarını sayfalı listele</summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _logService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Yönetici paneli için log özetini getir (toplam + son kayıt tarihi)</summary>
    [HttpGet("admin/summary")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _logService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için filtrelenmiş sayfalı log listesi</summary>
    [HttpGet("admin/paged")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? level,
        [FromQuery] string? project,
        [FromQuery] string? message,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _logService.GetAllForAdminPagedAsync(level, project, message, dateFrom, dateTo, pageNumber, pageSize, cancellationToken));

    /// <summary>Admin panelinin (AdminOnly JWT) sunucu-tarafı hata/uyarı logunu sistem log tablosuna yazar. Admin'in app-token'ı yoktur; ClientLog (AppClient) yerine bu uç nokta kullanılır.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLogRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return ToActionResult(Result.Fail("Log mesajı boş olamaz.", statusCode: 400));

        var result = await _logService.CreateAsync(new CreateLogDto
        {
            Project = Trim(StripControls(request.Project), MaxProject) ?? "FurkanTural_Admin",
            Date = _clock.UtcNow,
            Level = NormalizeLevel(request.Level),
            Message = Trim(StripControls(request.Message), MaxMessage),
            Detail = Trim(StripControls(request.Detail, keepWhitespace: true), MaxDetail),
            Path = Trim(StripControls(request.Path), MaxPath),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        return result.Success ? NoContent() : ToActionResult(result);
    }

    /// <summary><see cref="ClientLogController"/> içindeki eşiyle aynı kuralı uygular; sınırlar da aynı kolonlardan gelir. İkisi ayrışırsa aynı tabloya iki farklı temizlikten geçmiş kayıt düşer.</summary>
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
