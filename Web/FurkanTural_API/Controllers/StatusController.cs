using FurkanTural_API.Models.Common;
using FurkanTural_Application.DTOs.Common;
using Asp.Versioning;
using FurkanTural_Application.DTOs.Status;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Status;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiVersion("1.0")]
public class StatusController(IStatusService statusService) : JwtBaseController
{
    private readonly IStatusService _statusService = statusService;

    /// <summary>Statüyü ID ile getir</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _statusService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm statüleri listele</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _statusService.GetAllAsync(cancellationToken));

    /// <summary>Belirli gruba ait statüleri listele (örn. Friendship)</summary>
    [HttpGet("group/{group}")]
    public async Task<IActionResult> GetByGroup(string group, CancellationToken cancellationToken)
        => ToActionResult(await _statusService.GetByGroupAsync(group, cancellationToken));

    /// <summary>Statüleri sayfalı listele</summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _statusService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Tüm statüleri (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _statusService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Statüyü ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _statusService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Yeni statü ekle</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateStatusRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _statusService.CreateAsync(new CreateStatusDto
        {
            Group = request.Group,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            SortOrder = request.SortOrder,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Statüyü güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _statusService.UpdateAsync(new UpdateStatusDto
        {
            Id = request.Id,
            Group = request.Group,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            SortOrder = request.SortOrder,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Statüyü sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _statusService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Statünün aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _statusService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen statüyü geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _statusService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için statü özetini getir</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _statusService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı statü listesi</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? group,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _statusService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), group, cancellationToken));

    /// <summary>Yönetici paneli için statü durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? group,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _statusService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), group, cancellationToken));

    /// <summary>Seçili kayıtlara tek istekte uygulanır: siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _statusService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }
}