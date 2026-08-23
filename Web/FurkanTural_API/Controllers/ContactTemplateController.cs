using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.ContactTemplate;
using FurkanTural_Application.DTOs.ContactTemplate;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class ContactTemplateController(IContactTemplateService contactTemplateService) : JwtBaseController
{
    private readonly IContactTemplateService _contactTemplateService = contactTemplateService;

    /// <summary>Şablon ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactTemplateService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Tüm şablonları (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _contactTemplateService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Yeni şablon oluştur</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateContactTemplateRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _contactTemplateService.CreateAsync(new CreateContactTemplateDto
        {
            Name = request.Name,
            TemplateType = request.TemplateType,
            FileName = request.FileName,
            HtmlContent = request.HtmlContent,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Şablonu güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateContactTemplateRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _contactTemplateService.UpdateAsync(new UpdateContactTemplateDto
        {
            Id = request.Id,
            Name = request.Name,
            TemplateType = request.TemplateType,
            FileName = request.FileName,
            HtmlContent = request.HtmlContent,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Şablonu sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactTemplateService.DeleteAsync(id, cancellationToken));

    /// <summary>Şablonun aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactTemplateService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen şablonu geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactTemplateService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Admin özeti</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _contactTemplateService.GetAdminSummaryAsync(cancellationToken));
}
