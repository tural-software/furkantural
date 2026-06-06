using Asp.Versioning;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Controllers.Base;
using FurkanTural_API.Models.Friend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiVersion("1.0")]
public class FriendController(IUserFriendService userFriendService) : JwtBaseController
{
    private readonly IUserFriendService _userFriendService = userFriendService;

    /// <summary>Arkadaşlık isteği gönder</summary>
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestRequest request, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.SendRequestAsync(userId.Value, request.AddresseeId, cancellationToken));
    }

    /// <summary>Arkadaşlık isteğini kabul et</summary>
    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.AcceptRequestAsync(id, userId.Value, cancellationToken));
    }

    /// <summary>Arkadaşlık isteğini reddet</summary>
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.RejectRequestAsync(id, userId.Value, cancellationToken));
    }

    /// <summary>Arkadaşı kaldır</summary>
    [HttpDelete("{friendUserId:int}")]
    public async Task<IActionResult> Remove(int friendUserId, CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.RemoveFriendAsync(userId.Value, friendUserId, cancellationToken));
    }

    /// <summary>Arkadaş listemi getir</summary>
    [HttpGet]
    public async Task<IActionResult> GetFriends(CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.GetFriendsAsync(userId.Value, cancellationToken));
    }

    /// <summary>Bana gelen bekleyen arkadaşlık isteklerini getir</summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.GetPendingRequestsAsync(userId.Value, cancellationToken));
    }

    /// <summary>Gönderdiğim bekleyen (onay bekleyen) arkadaşlık isteklerini getir</summary>
    [HttpGet("requests/sent")]
    public async Task<IActionResult> GetSentRequests(CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.GetSentRequestsAsync(userId.Value, cancellationToken));
    }

    /// <summary>Bekleyen istek sayısını getir (rozet için)</summary>
    [HttpGet("requests/count")]
    public async Task<IActionResult> GetRequestCount(CancellationToken cancellationToken)
    {
        var userId = SortUserId();
        if (userId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.GetPendingRequestCountAsync(userId.Value, cancellationToken));
    }

    /// <summary>Bir kullanıcıyı engelle</summary>
    [HttpPost("{userId:int}/block")]
    public async Task<IActionResult> Block(int userId, CancellationToken cancellationToken)
    {
        var currentUserId = SortUserId();
        if (currentUserId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.BlockUserAsync(currentUserId.Value, userId, cancellationToken));
    }

    /// <summary>Bir kullanıcının engelini kaldır</summary>
    [HttpPost("{userId:int}/unblock")]
    public async Task<IActionResult> Unblock(int userId, CancellationToken cancellationToken)
    {
        var currentUserId = SortUserId();
        if (currentUserId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.UnblockUserAsync(currentUserId.Value, userId, cancellationToken));
    }

    /// <summary>Engellediğim kullanıcıları getir</summary>
    [HttpGet("blocked")]
    public async Task<IActionResult> GetBlocked(CancellationToken cancellationToken)
    {
        var currentUserId = SortUserId();
        if (currentUserId is null) return Unauthorized();
        return ToActionResult(await _userFriendService.GetBlockedAsync(currentUserId.Value, cancellationToken));
    }

    // ── Admin ──

    /// <summary>Tüm arkadaşlık kayıtlarını (admin) listele</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        => ToActionResult(await _userFriendService.GetAllForAdminAsync(cancellationToken));

    /// <summary>Arkadaşlık kaydını ID ile getir (admin)</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdForAdmin(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userFriendService.GetByIdForAdminAsync(id, cancellationToken));

    /// <summary>Arkadaşlık kaydının aktiflik durumunu değiştir</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userFriendService.ToggleActiveAsync(id, SortUserId(), cancellationToken));

    /// <summary>Silinen arkadaşlık kaydını geri yükle</summary>
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => ToActionResult(await _userFriendService.RestoreAsync(id, SortUserId(), cancellationToken));

    /// <summary>Yönetici paneli için arkadaşlık özetini getir</summary>
    [HttpGet("admin/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminSummary(CancellationToken cancellationToken)
        => ToActionResult(await _userFriendService.GetAdminSummaryAsync(cancellationToken));
}
