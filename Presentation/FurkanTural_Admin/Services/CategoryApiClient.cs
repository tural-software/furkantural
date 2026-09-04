using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Models.Category;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class CategoryApiClient(HttpClient httpClient, ILogger<CategoryApiClient> logger) : ICategoryApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CategoryApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<CategoryAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/category/admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kategori listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<CategoryAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<CategoryAdminDto>)[];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Kategori listesi uç noktasına erişilemedi.");
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kategori listesi alınırken beklenmeyen hata oluştu.");
            return [];
        }
    }

    public async Task<(IReadOnlyList<CategoryAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/category/admin/paged", paged: true));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kategori listesi alınamadı: {Status}", (int)response.StatusCode);
                return ([], 0);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<PagedApiResult<CategoryAdminDto>>(JsonOptions, ct);
            var rows = wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<CategoryAdminDto>)[];
            return (rows, wrapper?.TotalCount ?? 0);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Kategori listesi uç noktasına erişilemedi.");
            return ([], 0);
        }
        catch (TaskCanceledException)
        {
            return ([], 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kategori listesi alınırken beklenmeyen hata oluştu.");
            return ([], 0);
        }
    }

    public async Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/category/admin/counts", paged: false));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kategori sayaçları alınamadı: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<StatusCountsModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Kategori sayaç uç noktasına erişilemedi.");
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kategori sayaçları alınırken beklenmeyen hata oluştu.");
            return null;
        }
    }

    public async Task<bool> CreateAsync(CategoryFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/category");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(dto, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kategori oluşturulurken hata oluştu.");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(int id, CategoryFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/category");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var body = new { id, name = dto.Name, color = dto.Color };
            request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kategori güncellenirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/category/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kategori silinirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/category/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kategori aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/category/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kategori geri yüklenirken hata oluştu: {Id}", id);
            return false;
        }
    }
}