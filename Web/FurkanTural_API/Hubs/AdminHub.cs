using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FurkanTural_API.Hubs;

[Authorize(Policy = "AdminOnly")]
public class AdminHub(IAdminPendingWorkService pendingWork) : Hub
{
    private readonly IAdminPendingWorkService _pendingWork = pendingWork;

    public override async Task OnConnectedAsync()
    {
        if (int.TryParse(Context.UserIdentifier, out var userId))
            await Clients.Caller.SendAsync(AdminHubEvents.AdminSession, new AdminSessionDto(userId), Context.ConnectionAborted);

        await RefreshPendingWork();
        await base.OnConnectedAsync();
    }

    public async Task RefreshPendingWork()
    {
        var payload = await _pendingWork.GetAsync(string.Empty, Context.ConnectionAborted);
        await Clients.Caller.SendAsync(AdminHubEvents.PendingWorkChanged, payload, Context.ConnectionAborted);
    }
}
