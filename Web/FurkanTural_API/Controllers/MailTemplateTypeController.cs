using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.MailTemplateType;
using FurkanTural_Application.DTOs.MailTemplateType;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class MailTemplateTypeController(IMailTemplateTypeService mailTemplateTypeService) : JwtBaseController
{
    private readonly IMailTemplateTypeService _mailTemplateTypeService = mailTemplateTypeService;

    /// <summary>Posta türünü ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Tüm posta türlerini listele (admin)</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Etkin posta türlerini listele</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.GetAllAsync(cancellationToken));

    /// <summary>Yeni posta türü oluştur</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateMailTemplateTypeRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.CreateAsync(new CreateMailTemplateTypeDto
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Posta türünü güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateMailTemplateTypeRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.UpdateAsync(new UpdateMailTemplateTypeDto
        {
            Id = request.Id,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Posta türünü sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.DeleteAsync(id, cancellationToken));

    /// <summary>Posta türünün aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen posta türünü geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Admin özeti</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateTypeService.GetAdminSummaryAsync(cancellationToken));
}
