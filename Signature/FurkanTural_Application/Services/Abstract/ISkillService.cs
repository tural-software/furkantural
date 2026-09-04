using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Skill;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface ISkillService : IService<SkillDto, CreateSkillDto, UpdateSkillDto>
{
    Task<Result<IEnumerable<AdminSkillDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminSkillDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminSkillDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminSkillDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminSkillDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default);
}