using FurkanTural_API.Hubs;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.SignalR;

namespace FurkanTural_API.Realtime;

public class AdminNotifier(
    IHubContext<AdminHub> hubContext,
    IAdminPendingWorkService pendingWork,
    ILogger<AdminNotifier> logger) : IAdminNotifier
{
    private readonly IHubContext<AdminHub> _hubContext = hubContext;
    private readonly IAdminPendingWorkService _pendingWork = pendingWork;
    private readonly ILogger<AdminNotifier> _logger = logger;

    public async Task NotifyPendingWorkChangedAsync(string kind, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = await _pendingWork.GetAsync(kind, cancellationToken);
            await _hubContext.Clients.All.SendAsync("PendingWorkChanged", payload, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "Yönetici bildirimi gönderilemedi: {Kind}", kind);
        }
    }
}
