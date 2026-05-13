using FurkanTural_Admin.Models.Role;

namespace FurkanTural_Admin.Services;

public interface IRoleApiClient
{
    Task<IReadOnlyList<RoleAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
}
