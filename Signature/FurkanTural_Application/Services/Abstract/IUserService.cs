using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IUserService : IService<UserDto, CreateUserDto, UpdateUserDto>
{
    Task<Result<UserDto>> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> SeedAdminAsync(string? username, string? password, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<UserSearchResultDto>>> SearchAsync(string query, int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateAvatarAsync(int userId, string fileName, int? updatedBy, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının "son görülme" anını şimdiki UTC zamanına günceller ve o değeri döner.</summary>
    Task<DateTime> UpdateLastSeenAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Giriş yapan kullanıcının güncel üyelik sözleşmesini kabulünü kaydeder (eski üyeler için).</summary>
    Task<Result> AcceptAgreementAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminUserDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}