using Asp.Versioning;
using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Call;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiVersion("1.0")]
public class CallController(
    ITurnCredentialProvider turnCredentialProvider,
    ICallLogService callLogService,
    ICallPolicyService callPolicyService,
    IConfiguration configuration) : JwtBaseController
{
    private readonly ITurnCredentialProvider _turnCredentialProvider = turnCredentialProvider;
    private readonly ICallLogService _callLogService = callLogService;
    private readonly ICallPolicyService _callPolicyService = callPolicyService;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>WebRTC arama yapılandırması: ICE sunucuları + efektif video politikası + relay zorunluluğu. Kaynak <c>Calls:Ice:Mode</c> ile seçilir — "Static" (STUN/yerel, token'sız) veya "Cloudflare" (TURN).</summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();

        var mode = _configuration["Calls:Ice:Mode"] ?? "Cloudflare";
        var relayOnly = _configuration.GetValue<bool?>("Calls:Ice:RelayOnly") ?? true;

        IceServerDto[] servers;
        if (string.Equals(mode, "Static", StringComparison.OrdinalIgnoreCase))
        {
            servers = _configuration.GetSection("Calls:Ice:StaticServers").Get<IceServerDto[]>() ?? [];
            if (servers.Length == 0)
                servers = [new IceServerDto { Urls = ["stun:stun.cloudflare.com:3478"] }];
        }
        else
        {
            var ice = await _turnCredentialProvider.GetIceServersAsync(userId, cancellationToken);
            if (ice.IsFailure)
                return ToActionResult(ice);
            servers = ice.Data!.IceServers;
        }

        var policy = await _callPolicyService.GetEffectiveForUserAsync(userId.Value, cancellationToken);
        return ToActionResult(Result<CallConfigDto>.Ok(new CallConfigDto
        {
            IceServers = servers,
            VideoPolicy = policy,
            RelayOnly = relayOnly
        }));
    }

    /// <summary>Arama geçmişimi getir</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _callLogService.GetHistoryAsync(userId.Value, cancellationToken));
    }

    /// <summary>Arama (video) politikasını getir (admin)</summary>
    [HttpGet("policy")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPolicy(CancellationToken cancellationToken)
        => ToActionResult(await _callPolicyService.GetForAdminAsync(cancellationToken));

    /// <summary>Arama (video) politikasını güncelle (admin)</summary>
    [HttpPut("policy")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdateCallPolicyRequest request, CancellationToken cancellationToken)
        => ToActionResult(await _callPolicyService.UpdateAsync(new UpdateCallPolicyDto
        {
            BitrateLimitEnabled = request.BitrateLimitEnabled,
            MaxVideoBitrateKbps = request.MaxVideoBitrateKbps,
            MaxWidth = request.MaxWidth,
            MaxHeight = request.MaxHeight,
            MaxFps = request.MaxFps
        }, SortUserId(), cancellationToken));

    /// <summary>Tüm arama kayıtlarını (admin) sayfalı listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => ToActionResult(await _callLogService.GetAllPagedForAdminAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Arama kaydını ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _callLogService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Arama kaydının aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _callLogService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen arama kaydını geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _callLogService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için arama özetini getir</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _callLogService.GetAdminSummaryAsync(cancellationToken));
}
