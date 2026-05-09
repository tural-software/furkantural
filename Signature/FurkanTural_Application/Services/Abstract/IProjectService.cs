using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Project;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IProjectService : IService<ProjectDto, CreateProjectDto, UpdateProjectDto>
{
    Task<Result<IEnumerable<AdminProjectDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminProjectDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminProjectDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminProjectDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
