using FurkanTural_Application.DTOs.Category;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface ICategoryService : IService<CategoryDto, CreateCategoryDto, UpdateCategoryDto>
{
    Task<Result<IEnumerable<AdminCategoryDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminCategoryDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminCategoryDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminCategoryDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminCategoryDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default);
}