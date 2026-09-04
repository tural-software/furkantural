using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.ProjectImage;

namespace FurkanTural_Admin.Services;

public interface IProjectImageApiClient
{
    Task<IReadOnlyList<ProjectImageAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<(IReadOnlyList<ProjectImageAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<int?> CreateAsync(IFormFile imageFile, string? altText, bool isCover, int projectId, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, IFormFile? imageFile, string? altText, bool isCover, int projectId, string token, CancellationToken ct = default);
    Task<ProjectImageAdminDto?> GetByIdForAdminAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
}