using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.User;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IUserService : IService<UserDto, CreateUserDto, UpdateUserDto>
{
    Task<Result<UserDto>> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminUserDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}