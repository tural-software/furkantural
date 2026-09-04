using FurkanTural_Application.DTOs.BlogImage;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IBlogImageService : IService<BlogImageDto, CreateBlogImageDto, UpdateBlogImageDto>
{
    Task<Result<IEnumerable<BlogImageDto>>> GetByBlogIdAsync(int blogId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminBlogImageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminBlogImageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminBlogImageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminBlogImageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminBlogImageDto>> GetAllForAdminPagedAsync(AdminListQuery query, bool? isCover, int? blogId, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, bool? isCover, int? blogId, CancellationToken cancellationToken = default);
}