using Asp.Versioning;
using FurkanTural_Application.DTOs.Push;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Push;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiVersion("1.0")]
public class PushController(IPushSubscriptionService pushSubscriptionService) : JwtBaseController
{
    private readonly IPushSubscriptionService _pushSubscriptionService = pushSubscriptionService;

    /// <summary>İstemcinin pushManager.subscribe için kullanacağı VAPID açık anahtarını döndürür.</summary>
    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var key = _pushSubscriptionService.GetVapidPublicKey();
        return key is null
            ? ToActionResult(Result<string>.Fail("Push bildirimleri yapılandırılmamış.", statusCode: 503))
            : ToActionResult(Result<string>.Ok(key));
    }

    /// <summary>Giriş yapan kullanıcının bu cihaz için push aboneliğini kaydeder (varsa günceller).</summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();

        return ToActionResult(await _pushSubscriptionService.SubscribeAsync(userId.Value, new PushSubscriptionDto
        {
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth,
            UserAgent = request.UserAgent
        }, cancellationToken));
    }

    /// <summary>Bu cihazın push aboneliğini kaldırır (kullanıcı bildirimleri kapattığında).</summary>
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();

        return ToActionResult(await _pushSubscriptionService.UnsubscribeAsync(userId.Value, request.Endpoint, cancellationToken));
    }
}
