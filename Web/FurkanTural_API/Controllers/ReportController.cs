using FurkanTural_API.Models.Common;
using FurkanTural_Application.DTOs.Common;
using Asp.Versioning;
using FurkanTural_Application.DTOs.Report;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiVersion("1.0")]
public class ReportController(IReportService reportService) : JwtBaseController
{
    private readonly IReportService _reportService = reportService;

    /// <summary>Bir kullanıcı/mesaj/medya/aramayı şikayet et</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();

        return ToActionResult(await _reportService.CreateAsync(userId.Value, new CreateReportDto
        {
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            ReportedUserId = request.ReportedUserId,
            Reason = request.Reason
        }, cancellationToken));
    }

    /// <summary>Tüm şikayetleri (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _reportService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Şikayeti ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _reportService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Şikayet durumunu güncelle (admin)</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReportStatusRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _reportService.UpdateStatusAsync(id, request.Status, request.AdminNote, SortUserId(), cancellationToken));

    /// <summary>Şikayetin aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _reportService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen şikayeti geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _reportService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için şikayet özetini getir</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _reportService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı şikayet listesi</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? targetType,
        [FromQuery] string? status,
        [FromQuery] string[]? statuses,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _reportService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), targetType, status, statuses, cancellationToken));

    /// <summary>Yönetici paneli için şikayet durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? targetType,
        [FromQuery] string? status,
        [FromQuery] string[]? statuses,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _reportService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), targetType, status, statuses, cancellationToken));

    /// <summary>Seçili kayıtlara tek istekte uygulanır: siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _reportService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }
}