using FurkanTural_Admin.Models.Call;

namespace FurkanTural_Admin.Services;

public interface ICallLogApiClient
{
    Task<IReadOnlyList<CallLogAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}
