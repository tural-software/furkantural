using FurkanTural_Admin.Models.Project;

namespace FurkanTural_Admin.Services;

public interface IProjectApiClient
{
    Task<IReadOnlyList<ProjectAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> CreateAsync(ProjectFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, ProjectFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}
