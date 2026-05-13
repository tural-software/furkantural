using FurkanTural_Admin.Models.Education;

namespace FurkanTural_Admin.Services;

public interface IEducationApiClient
{
    Task<IReadOnlyList<EducationAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> CreateAsync(EducationFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, EducationFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}
