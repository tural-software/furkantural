using FurkanTural_Admin.Models.Role;

namespace FurkanTural_Admin.Services;

public interface IRoleApiClient
{
    Task<IReadOnlyList<RoleAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> CreateAsync(RoleFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, RoleFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}
