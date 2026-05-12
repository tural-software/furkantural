using FurkanTural_Admin.Models.Subscriber;

namespace FurkanTural_Admin.Services;

public interface ISubscriberApiClient
{
    Task<IReadOnlyList<SubscriberAdminDto>> GetAllForAdminAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken cancellationToken = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken cancellationToken = default);
}
