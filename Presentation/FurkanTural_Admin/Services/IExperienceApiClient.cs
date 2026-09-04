using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Experience;

namespace FurkanTural_Admin.Services;

public interface IExperienceApiClient
{
    Task<IReadOnlyList<ExperienceAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<(IReadOnlyList<ExperienceAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<bool> CreateAsync(ExperienceFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, ExperienceFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}