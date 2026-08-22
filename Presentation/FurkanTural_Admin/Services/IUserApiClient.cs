using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.User;

namespace FurkanTural_Admin.Services;

public interface IUserApiClient
{
    Task<IReadOnlyList<UserAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<ApiCallResult> CreateAsync(UserFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> UpdateAsync(int id, UserFormDto dto, string token, CancellationToken ct = default);
    Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default);
    Task<ApiCallResult> UploadAvatarAsync(int id, IFormFile file, string token, CancellationToken ct = default);
}