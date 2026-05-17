using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiVersion("1.0")]
public class LogController(ILogService logService) : BaseApiController
{
    private readonly ILogService _logService = logService;

    /// <summary>
    /// Sistem logunu ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _logService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm sistem loglarını listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _logService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Sistem loglarını sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _logService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Yönetici paneli için log özetini getir (toplam + son kayıt tarihi)
    /// </summary>
    [HttpGet("admin/summary")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _logService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>
    /// Yönetici paneli için filtrelenmiş sayfalı log listesi
    /// </summary>
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
}