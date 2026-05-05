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

    /// <summary>
    /// Aboneyi ID ile getir
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Tüm aboneleri listele
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Aboneleri sayfalı listele
    /// </summary>
    [HttpGet("paged")]
    [Authorize]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        => ToActionResult(await _subscriberService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>
    /// Bültene abone ol
    /// </summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.SubscribeAsync(request.Email ?? string.Empty, cancellationToken));

    /// <summary>
    /// Bülten aboneliğini iptal et
    /// </summary>
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.UnsubscribeAsync(request.Email ?? string.Empty, cancellationToken));

    /// <summary>
    /// Aboneyi sistemden sil
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Silinen aboneyi geri yükle
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _subscriberService.RestoreAsync(id, SortUserId(), cancellationToken));
}