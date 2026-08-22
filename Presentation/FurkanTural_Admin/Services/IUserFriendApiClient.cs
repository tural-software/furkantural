using FurkanTural_Admin.Models.UserFriend;

namespace FurkanTural_Admin.Services;

public interface IUserFriendApiClient
{
    Task<IReadOnlyList<UserFriendAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default);
}