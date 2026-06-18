using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Models.Wrappers;

namespace FurkanTural_Blog.Services;

public class BlogApiService(HttpClient httpClient, ILogger<BlogApiService> logger) : IBlogApiService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<BlogApiService> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<BlogPostViewModel>> GetPostsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<BlogPostViewModel>>>("/api/v1/blog", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<BlogPostViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blog yazıları alınamadı.");
            return [];
        }
    }

    public async Task<BlogPostViewModel?> GetPostAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<BlogPostViewModel>>($"/api/v1/blog/{id}", JsonOptions, ct);
            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blog yazısı alınamadı. Id={Id}", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<BlogImageViewModel>> GetAllImagesAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<BlogImageViewModel>>>("/api/v1/blogimage", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<BlogImageViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blog görselleri alınamadı.");
            return [];
        }
    }

    public async Task<IReadOnlyList<BlogImageViewModel>> GetImagesByBlogAsync(int blogId, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<BlogImageViewModel>>>($"/api/v1/blogimage/by-blog/{blogId}", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<BlogImageViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blog görselleri alınamadı. BlogId={BlogId}", blogId);
            return [];
        }
    }
}
