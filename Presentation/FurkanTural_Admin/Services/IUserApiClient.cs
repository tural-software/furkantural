using FurkanTural_Admin.Models.User;

namespace FurkanTural_Admin.Services;

public interface IUserApiClient
{
    Task<IReadOnlyList<UserAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> CreateAsync(UserFormDto dto, string token, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, UserFormDto dto, string token, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
    Task<bool> UploadAvatarAsync(int id, IFormFile file, string token, CancellationToken ct = default);
}
