using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Blog yazıları. Ayrı bir "yayınlandı" alanı yoktur, yayın durumu canlı satır olmakla aynı şeydir; bu yüzden tabandan gelen GetAllPagedAsync doğrudan GetPublishedPagedAsync'e filtresiz devreder ve yazıyı pasife almak onu siteden kaldırmakla eşdeğerdir. Sıralama ile kategori ve arama filtreleri veri tabanında, sayfalama ile aynı sorguda uygulanır. GetSitemapAsync yalnızca kimlik ve tarih taşıyan dar bir izdüşümdür, içerik çekmez.</summary>
public interface IBlogService : IService<BlogDto, CreateBlogDto, UpdateBlogDto>
{
    Task<PagedResult<BlogDto>> GetPublishedPagedAsync(int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<BlogSitemapDto>>> GetSitemapAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminBlogDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminBlogDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminBlogDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminBlogDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminBlogDto>> GetAllForAdminPagedAsync(AdminListQuery query, int? blogId, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, int? blogId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AdminOptionDto>>> GetAdminOptionsAsync(string? search, int? take, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminBlogDto>> GetAllForAdminPagedAsync(AdminListQuery query, int? blogId, bool includeContent, CancellationToken cancellationToken = default);
}
