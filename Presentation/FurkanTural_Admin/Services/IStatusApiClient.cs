using FurkanTural_Admin.Models.Status;

namespace FurkanTural_Admin.Services;

public interface IStatusApiClient
{
    Task<IReadOnlyList<StatusAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> CreateAsync(StatusFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, StatusFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}