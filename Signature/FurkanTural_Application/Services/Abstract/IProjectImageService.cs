using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.ProjectImage;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IProjectImageService : IService<ProjectImageDto, CreateProjectImageDto, UpdateProjectImageDto>
{
    Task<Result<IEnumerable<ProjectImageDto>>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminProjectImageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminProjectImageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminProjectImageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminProjectImageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminProjectImageDto>> GetAllForAdminPagedAsync(AdminListQuery query, bool? isCover, int? projectId, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, bool? isCover, int? projectId, CancellationToken cancellationToken = default);
}