using FurkanTural_Application.DTOs.Education;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Education;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class EducationController(IEducationService educationService) : JwtBaseController
{
    private readonly IEducationService _educationService = educationService;

    /// <summary>
    /// Eğitim bilgisini ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _educationService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm eğitim bilgilerini listele
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _educationService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Eğitim bilgilerini sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _educationService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));
    /// <summary>
    /// Tüm eğitim bilgilerini yönetici paneli için listele (silinmişler dahil)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _educationService.GetAllForAdminAsync(cancellationToken));

    /// <summary>
    /// Eğitim bilgisini yönetici paneli için ID ile getir (silinmiş dahil)
    /// </summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _educationService.GetByIdForAdminAsync(id, cancellationToken));
    /// <summary>
    /// Yeni eğitim bilgisi ekle
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateEducationRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _educationService.CreateAsync(new CreateEducationDto
        {
            Institution = request.Institution,
            Degree = request.Degree,
            FieldOfStudy = request.FieldOfStudy,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Eğitim bilgisini güncelle
    /// </summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateEducationRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _educationService.UpdateAsync(new UpdateEducationDto
        {
            Id = request.Id,
            Institution = request.Institution,
            Degree = request.Degree,
            FieldOfStudy = request.FieldOfStudy,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>
    /// Eğitim bilgisini sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _educationService.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Eğitim bilgisinin aktiflik durumunu değiştir
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _educationService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Silinen eğitim bilgisini geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _educationService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>
    /// Yönetici paneli için eğitim özetini getir (toplam + son işlem tarihi)
    /// </summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _educationService.GetAdminSummaryAsync(cancellationToken));
}