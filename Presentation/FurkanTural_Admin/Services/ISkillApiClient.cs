using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Skill;

namespace FurkanTural_Admin.Services;

public interface ISkillApiClient
{
    Task<IReadOnlyList<SkillAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<ApiCallResult> CreateAsync(SkillFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> UpdateAsync(int id, SkillFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default);
}