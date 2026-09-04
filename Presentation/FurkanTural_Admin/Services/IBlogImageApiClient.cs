using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.BlogImage;

namespace FurkanTural_Admin.Services;

public interface IBlogImageApiClient
{
    Task<IReadOnlyList<BlogImageAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<(IReadOnlyList<BlogImageAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default);
    Task<int?> CreateAsync(IFormFile imageFile, string? altText, bool isCover, int blogId, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, IFormFile? imageFile, string? altText, bool isCover, int blogId, string token, CancellationToken ct = default);
    Task<BlogImageAdminDto?> GetByIdForAdminAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
}