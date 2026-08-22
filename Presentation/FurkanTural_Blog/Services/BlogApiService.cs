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

    public async Task<PagedPostsViewModel> GetPostsPagedAsync(int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken ct = default)
    {
        var categories = await GetCategoriesAsync(ct);
        try
        {
            var url = $"/api/v1/blog/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (categoryId is int cid)
                url += $"&categoryId={cid}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search.Trim())}";

            var result = await _httpClient.GetFromJsonAsync<PagedApiResult<IEnumerable<BlogPostViewModel>>>(url, JsonOptions, ct);

            var items = result?.Data?.ToList() ?? [];
            var size = result is { PageSize: > 0 } ? result.PageSize : pageSize;
            var total = result?.TotalCount ?? items.Count;
            return new PagedPostsViewModel
            {
                Items = items.AsReadOnly(),
                PageNumber = result is { PageNumber: > 0 } ? result.PageNumber : pageNumber,
                PageSize = size,
                TotalCount = total,
                TotalPages = size > 0 ? (int)Math.Ceiling(total / (double)size) : 0,
                CategoryId = categoryId,
                Search = search,
                AvailableCategories = categories
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blog yazıları (sayfalı) alınamadı. Sayfa={Page}", pageNumber);
            return new PagedPostsViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                CategoryId = categoryId,
                Search = search,
                AvailableCategories = categories
            };
        }
    }

    public async Task<IReadOnlyList<CategoryViewModel>> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<CategoryViewModel>>>("/api/v1/category", JsonOptions, ct);
            return result?.Data?
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .OrderBy(c => c.Name)
                .ToList().AsReadOnly() ?? (IReadOnlyList<CategoryViewModel>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kategoriler alınamadı.");
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

    public async Task<IReadOnlyList<BlogSitemapItem>> GetSitemapItemsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<ApiResult<IEnumerable<BlogSitemapItem>>>("/api/v1/blog/sitemap", JsonOptions, ct);
            return result?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<BlogSitemapItem>)[];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sitemap verisi alınamadı.");
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