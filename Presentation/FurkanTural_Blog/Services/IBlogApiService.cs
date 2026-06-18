using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Services;

public interface IBlogApiService
{
    Task<IReadOnlyList<BlogPostViewModel>> GetPostsAsync(CancellationToken ct = default);
    Task<BlogPostViewModel?> GetPostAsync(int id, CancellationToken ct = default);

    /// <summary>Tüm blog görselleri (liste sayfasında kapak haritası için tek çağrı).</summary>
    Task<IReadOnlyList<BlogImageViewModel>> GetAllImagesAsync(CancellationToken ct = default);

    /// <summary>Belirli bir bloğa ait görseller (detay sayfası kapağı/galerisi için).</summary>
    Task<IReadOnlyList<BlogImageViewModel>> GetImagesByBlogAsync(int blogId, CancellationToken ct = default);
}
