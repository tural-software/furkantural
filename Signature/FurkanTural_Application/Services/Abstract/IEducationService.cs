using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Education;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IEducationService : IService<EducationDto, CreateEducationDto, UpdateEducationDto>
{
    Task<Result<IEnumerable<AdminEducationDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminEducationDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminEducationDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminEducationDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminEducationDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? degree, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? degree, CancellationToken cancellationToken = default);
}