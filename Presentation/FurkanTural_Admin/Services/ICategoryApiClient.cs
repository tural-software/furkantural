using FurkanTural_Admin.Models.Category;

namespace FurkanTural_Admin.Services;

public interface ICategoryApiClient
{
    Task<IReadOnlyList<CategoryAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> CreateAsync(CategoryFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, CategoryFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}