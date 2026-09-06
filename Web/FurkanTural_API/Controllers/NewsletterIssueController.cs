using Asp.Versioning;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Common;
using FurkanTural_API.Models.NewsletterIssue;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Newsletter;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

/// <summary>Bülten sayılarının yönetimi. Uçların tamamı <c>AdminOnly</c>'dir ve ziyaretçiye açık karşılığı yoktur; bültene abone olma ve çıkma akışı <see cref="SubscriberController"/> tarafındadır.</summary>
[ApiVersion("1.0")]
public class NewsletterIssueController(INewsletterIssueService newsletterIssueService) : JwtBaseController
{
    private readonly INewsletterIssueService _newsletterIssueService = newsletterIssueService;

    /// <summary>Bülteni ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Yönetici paneli için süzülmüş ve sayfalı bülten listesi</summary>
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
        => ToActionResult(await _newsletterIssueService.GetAllForAdminPagedAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo, pageNumber, pageSize), cancellationToken));

    /// <summary>Yönetici paneli için bülten durum sayaçları; süzgeçler sayfalı listeyle aynıdır</summary>
    [HttpGet("admin/counts")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminCounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeleted,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken = default)
        => ToActionResult(await _newsletterIssueService.GetAdminStatusCountsAsync(
            AdminListQuery.From(search, isActive, isDeleted, dateFrom, dateTo), cancellationToken));

    /// <summary>Yönetici paneli için bülten özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.GetAdminSummaryAsync(cancellationToken));

    /// <summary>Yeni bülten taslağı oluştur</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] NewsletterIssueRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.CreateAsync(
            new CreateNewsletterIssueDto { Subject = request.Subject, Body = request.Body }, SortUserId(), cancellationToken));

    /// <summary>Bülten taslağını güncelle. Dağıtıma verilmiş bir sayının metni değiştirilemez.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] NewsletterIssueRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.UpdateAsync(
            new UpdateNewsletterIssueDto { Id = id, Subject = request.Subject, Body = request.Body }, SortUserId(), cancellationToken));

    /// <summary>Bülteni sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.DeleteAsync(id, SortUserId(), cancellationToken));

    /// <summary>Bültenin aktiflik durumunu değiştir. Dağıtımı süren bir sayıda bu aynı zamanda duraklat/sürdür anlamına gelir.</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen bülteni geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Bülteni dağıtıma ver. Alıcı listesi bu anda dondurulur; gönderim arka planda sürer ve istek onu beklemez.</summary>
    [HttpPost("{id:int}/queue")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Queue(int id, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.QueueAsync(id, SortUserId(), cancellationToken));

    /// <summary>Şu anda dağıtıma girecek doğrulanmış adres sayısını getir</summary>
    [HttpGet("admin/audience")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Audience(CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.GetAudienceCountAsync(cancellationToken));

    /// <summary>Dağıtımın anlık ilerlemesini getir</summary>
    [HttpGet("{id:int}/progress")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Progress(int id, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.GetProgressAsync(id, cancellationToken));

    /// <summary>Bülteni tek bir adrese deneme olarak gönder. Dağıtım satırı açmaz, durumu ve sayaçları değiştirmez.</summary>
    [HttpPost("{id:int}/test")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SendTest(int id, [FromBody] NewsletterTestRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _newsletterIssueService.SendTestAsync(id, request.Email, cancellationToken));

    /// <summary>Seçili kayıtlara tek istekte uygulanır: siler, geri yükler, aktife ya da pasife alır. Uygun durumda olmayan kayıtlar atlanır ve yanıtta listelenir; en çok 100 kimlik</summary>
    [HttpPost("admin/bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<BulkAction>(request.Action, ignoreCase: true, out var action))
            return BadRequest(new { success = false, statusCode = 400, errors = new[] { "Geçersiz toplu işlem türü." } });

        return ToActionResult(await _newsletterIssueService.BulkAsync(action, request.Ids ?? [], SortUserId(), cancellationToken));
    }
}
