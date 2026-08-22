using FurkanTural_Admin.Models.Blog;

namespace FurkanTural_Admin.Services;

public interface IBlogApiClient
{
    Task<IReadOnlyList<BlogAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> CreateAsync(BlogFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, BlogFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}