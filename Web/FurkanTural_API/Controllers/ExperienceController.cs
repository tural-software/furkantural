using FurkanTural_Application.DTOs.Experience;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Experience;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class ExperienceController(IExperienceService experienceService) : JwtBaseController
{
    private readonly IExperienceService _experienceService = experienceService;

    /// <summary>Tecrübeyi ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm tecrübeleri listele</summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.GetAllAsync(cancellationToken));

    /// <summary>Tüm tecrübeleri (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Tecrübeyi ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Tecrübeleri sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _experienceService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Yeni tecrübe ekle</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateExperienceRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.CreateAsync(new CreateExperienceDto
        {
            Position = request.Position,
            CompanyName = request.CompanyName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Tecrübeyi güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateExperienceRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.UpdateAsync(new UpdateExperienceDto
        {
            Id = request.Id,
            Position = request.Position,
            CompanyName = request.CompanyName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Tecrübeyi sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.DeleteAsync(id, cancellationToken));

    /// <summary>Tecrübenin aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen tecrübeyi geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için tecrübe özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _experienceService.GetAdminSummaryAsync(cancellationToken));
}
