using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IBlogService : IService<BlogDto, CreateBlogDto, UpdateBlogDto>
{
    /// <summary>Yayınlanmış yazıları en yeni en üstte, isteğe bağlı kategori + başlık aramasıyla sayfalar.</summary>
    Task<PagedResult<BlogDto>> GetPublishedPagedAsync(int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken cancellationToken = default);

    /// <summary>Sitemap/SEO için yayınlı yazıların hafif listesi (Id + tarihler, içerik yok).</summary>
    Task<Result<IEnumerable<BlogSitemapDto>>> GetSitemapAsync(CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<AdminBlogDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminBlogDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminBlogDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminBlogDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}