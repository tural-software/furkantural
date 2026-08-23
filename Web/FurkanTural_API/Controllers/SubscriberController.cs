using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Subscriber;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FurkanTural_API.Controllers;

[ApiVersion("1.0")]
public class SubscriberController(ISubscriberService subscriberService) : JwtBaseController
{
    private readonly ISubscriberService _subscriberService = subscriberService;

    /// <summary>Aboneyi ID ile getir</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.GetByIdAsync(id, cancellationToken));

    /// <summary>Tüm aboneleri listele</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.GetAllAsync(cancellationToken));

    /// <summary>Tüm aboneleri (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Aboneyi ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Aboneleri sayfalı listele</summary>
    [HttpGet("paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _subscriberService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Bültene abone ol</summary>
    [HttpPost("subscribe")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.SubscribeAsync(request.Email ?? string.Empty, cancellationToken));

    /// <summary>Bülten aboneliğini iptal et</summary>
    [HttpPost("unsubscribe")]
    [Authorize(Policy = "VisitorOrAbove")]
    public async Task<IActionResult> Unsubscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.UnsubscribeAsync(request.Email ?? string.Empty, cancellationToken));

    /// <summary>Aboneyi sistemden sil</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.DeleteAsync(id, cancellationToken));

    /// <summary>Abonelinin aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen aboneyi geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için abone özetini getir (toplam + son işlem tarihi)</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.GetAdminSummaryAsync(cancellationToken));
}
