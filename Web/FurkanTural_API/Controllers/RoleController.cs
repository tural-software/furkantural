using FurkanTural_Application.DTOs.Role;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiVersion("1.0")]
public class RoleController(IRoleService roleService) : JwtBaseController
{
    private readonly IRoleService _roleService = roleService;

    /// <summary>
    /// Rolü ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _roleService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm rolleri listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _roleService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Rolleri sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _roleService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Tüm rolleri (admin) listele
    /// </summary>
    [HttpGet("admin")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _roleService.GetAllForAdminAsync(cancellationToken));

    /// <summary>
    /// Rolü ID ile getir (admin)
    /// </summary>
    [HttpGet("admin/{id:int}")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _roleService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>
    /// Yeni rol ekle
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _roleService.CreateAsync(new CreateRoleDto
        {
            Name = request.Name,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Rolü güncelle
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _roleService.UpdateAsync(new UpdateRoleDto
        {
            Id = request.Id,
            Name = request.Name,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Rolü sil
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _roleService.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Rolün aktiflik durumunu değiştir
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _roleService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Silinen rolü geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _roleService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Yönetici paneli için rol özetini getir (toplam + son işlem tarihi)
    /// </summary>
    [HttpGet("admin/summary")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _roleService.GetAdminSummaryAsync(cancellationToken));
}