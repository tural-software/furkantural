namespace FurkanTural_Application.Services.Abstract;

public interface IAdminNotifier
{
    Task NotifyPendingWorkChangedAsync(string kind, CancellationToken cancellationToken = default);
}
