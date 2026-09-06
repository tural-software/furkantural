using FurkanTural_API.Models.Common;
using FurkanTural_Application.DTOs.Common;
using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.MailTemplate;
using FurkanTural_Application.DTOs.MailTemplate;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class MailTemplateController(IMailTemplateService mailTemplateService) : JwtBaseController
{
    private readonly IMailTemplateService _mailTemplateService = mailTemplateService;

    /// <summary>Posta şablonunu ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Tüm posta şablonlarını listele (admin)</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Yeni posta şablonu oluştur</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateMailTemplateRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateService.CreateAsync(new CreateMailTemplateDto
        {
            MailTemplateTypeId = request.MailTemplateTypeId,
            AppSourceId = request.AppSourceId,
            Name = request.Name,
            Subject = request.Subject,
            HtmlContent = request.HtmlContent,
            FileName = request.FileName,
            CreatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Posta şablonunu güncelle</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateMailTemplateRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateService.UpdateAsync(new UpdateMailTemplateDto
        {
            Id = request.Id,
            MailTemplateTypeId = request.MailTemplateTypeId,
            AppSourceId = request.AppSourceId,
            Name = request.Name,
            Subject = request.Subject,
            HtmlContent = request.HtmlContent,
            FileName = request.FileName,
            UpdatedBy = SortUserId()
        }, cancellationToken));

    /// <summary>Posta şablonunu sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Posta şablonunun aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen posta şablonunu geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Admin özeti</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _mailTemplateService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı posta şablonu listesi</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _mailTemplateService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), cancellationToken));

    /// <summary>Yönetici paneli için posta şablonu durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _mailTemplateService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), cancellationToken));

    /// <summary>Seçili kayıtlara tek istekte uygulanır: siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _mailTemplateService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }
}
