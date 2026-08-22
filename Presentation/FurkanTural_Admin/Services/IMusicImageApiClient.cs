using FurkanTural_Admin.Models.MusicImage;

namespace FurkanTural_Admin.Services;

public interface IMusicImageApiClient
{
    Task<IReadOnlyList<MusicImageAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<int?> CreateAsync(IFormFile imageFile, string? altText, bool isCover, int musicId, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, IFormFile? imageFile, string? altText, bool isCover, int musicId, string token, CancellationToken ct = default);
    Task<MusicImageAdminDto?> GetByIdForAdminAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
}