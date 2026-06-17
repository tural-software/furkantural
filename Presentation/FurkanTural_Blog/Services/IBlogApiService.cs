using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Services;

public interface IBlogApiService
{
    Task<IReadOnlyList<BlogPostViewModel>> GetPostsAsync(CancellationToken ct = default);
    Task<BlogPostViewModel?> GetPostAsync(int id, CancellationToken ct = default);
}
