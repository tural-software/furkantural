using System.Security.Claims;
using System.Threading.Channels;
using FurkanTural_API.Hubs;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FurkanTural_API.Realtime;

public sealed class AdminLiveBroadcaster(
    IHubContext<AdminHub> hubContext,
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    IClock clock,
    ILogger<AdminLiveBroadcaster> logger) : BackgroundService, IAdminChangeFeed
{
    private readonly record struct Pending(string Kind, int ActorId, int Added, int Changed, DateTime At);

    private readonly IHubContext<AdminHub> _hubContext = hubContext;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IClock _clock = clock;
    private readonly ILogger<AdminLiveBroadcaster> _logger = logger;
    private readonly AdminChangeWindows _windows = new();
    private readonly Channel<Pending> _queue = Channel.CreateUnbounded<Pending>(new UnboundedChannelOptions { SingleReader = true });

    public static int ActorIdOf(ClaimsPrincipal? user)
    {
        if (user is null || !user.IsInRole("Admin"))
            return 0;

        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(sub, out var id) ? id : 0;
    }

    public void Publish(IReadOnlyList<AdminEntityChange> changes)
    {
        if (changes.Count == 0)
            return;

        var actorId = ActorIdOf(_httpContextAccessor.HttpContext?.User);
        var now = _clock.UtcNow;

        foreach (var change in changes)
            _queue.Writer.TryWrite(new Pending(change.Kind, actorId, change.Added, change.Changed, now));
    }

    public async Task FlushAsync(IReadOnlyList<AdminListChangeDto> due, CancellationToken cancellationToken)
    {
        if (due.Count == 0)
            return;

        try
        {
            await _hubContext.Clients.All.SendAsync(AdminHubEvents.ListsChanged, new AdminListsChangedDto(due), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Yönetici liste değişikliği gönderilemedi.");
        }

        if (!due.Any(d => AdminListKinds.Work.Contains(d.Kind)))
            return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var pendingWork = scope.ServiceProvider.GetRequiredService<IAdminPendingWorkService>();
            var payload = await pendingWork.GetAsync(string.Empty, cancellationToken);
            await _hubContext.Clients.All.SendAsync(AdminHubEvents.PendingWorkChanged, payload, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Bekleyen iş sayıları gönderilemedi.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task<bool>? incoming = null;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (_queue.Reader.TryRead(out var item))
                    _windows.Add(item.Kind, item.ActorId, item.Added, item.Changed, item.At);

                await FlushAsync(_windows.TakeDue(_clock.UtcNow), stoppingToken);

                incoming ??= _queue.Reader.WaitToReadAsync(stoppingToken).AsTask();

                var nextDueAt = _windows.NextDueAt;
                if (nextDueAt is null)
                {
                    if (!await incoming)
                        return;

                    incoming = null;
                    continue;
                }

                var delay = nextDueAt.Value - _clock.UtcNow;
                if (delay <= TimeSpan.Zero)
                    continue;

                var completed = await Task.WhenAny(incoming, Task.Delay(delay, stoppingToken));
                if (completed != incoming)
                    continue;

                if (!await incoming)
                    return;

                incoming = null;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
