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
}