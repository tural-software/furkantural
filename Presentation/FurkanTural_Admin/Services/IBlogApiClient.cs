using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Blog;

namespace FurkanTural_Admin.Services;

public interface IBlogApiClient
{
    Task<IReadOnlyList<BlogAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<(IReadOnlyList<BlogAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<bool> CreateAsync(BlogFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, BlogFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);

    Task<IReadOnlyList<AdminOptionDto>> GetAdminOptionsAsync(string? search, int? take, string token, CancellationToken ct = default);

    Task<BlogAdminDto?> GetByIdForAdminAsync(int id, string token, CancellationToken ct = default);

    Task<BulkResultModel?> BulkAsync(string action, IReadOnlyList<int> ids, string token, CancellationToken ct = default);
}