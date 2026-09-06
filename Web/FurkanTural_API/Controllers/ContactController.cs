using FurkanTural_API.Models.Common;
using FurkanTural_Application.DTOs.Common;
using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Contact;
using FurkanTural_Application.DTOs.Contact;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class ContactController(IContactService contactService) : JwtBaseController
{
    private readonly IContactService _contactService = contactService;

    /// <summary>İletişim formunu gönder</summary>
    [HttpPost("submit")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> Submit([FromBody] SubmitContactRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        return ToActionResult(await _contactService.SubmitAsync(new SubmitContactDto
        {
            Name = request.Name,
            Email = request.Email,
            Message = request.Message,
            TurnstileToken = request.TurnstileToken
        }, ip, userAgent, cancellationToken));
    }

    /// <summary>İletişim mesajı ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Tüm iletişim mesajlarını (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _contactService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Mesajı okundu olarak işaretle</summary>
    [HttpPatch("{id:int}/mark-as-read")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactService.MarkAsReadAsync(id, cancellationToken));

    /// <summary>Mesajı sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Mesajın aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen mesajı geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _contactService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Admin özeti</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _contactService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı iletişim mesajı listesi</summary>
    [HttpGet("admin/paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] bool? isRead,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _contactService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), isRead, cancellationToken));

    /// <summary>Yönetici paneli için iletişim mesajı durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] bool? isRead,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _contactService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), isRead, cancellationToken));

    /// <summary>Seçili kayıtlara tek istekte uygulanır: siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _contactService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }
}
