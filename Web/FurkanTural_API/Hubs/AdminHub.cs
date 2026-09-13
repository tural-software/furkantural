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
        await RefreshPendingWork();
        await base.OnConnectedAsync();
    }

    public async Task RefreshPendingWork()
    {
        var payload = await _pendingWork.GetAsync(string.Empty, Context.ConnectionAborted);
        await Clients.Caller.SendAsync("PendingWorkChanged", payload, Context.ConnectionAborted);
    }
}
