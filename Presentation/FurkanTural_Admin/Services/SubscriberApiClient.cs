using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.Subscriber;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class SubscriberApiClient(HttpClient httpClient, ILogger<SubscriberApiClient> logger) : ISubscriberApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<SubscriberApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<SubscriberAdminDto>> GetAllForAdminAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/subscriber/admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Abone listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<SubscriberAdminDto>>>(JsonOptions, cancellationToken);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<SubscriberAdminDto>)[];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Abone listesi uç noktasına erişilemedi.");
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Abone listesi alınırken beklenmeyen hata oluştu.");
            return [];
        }
    }

    public async Task<(IReadOnlyList<SubscriberAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/subscriber/admin/paged", paged: true));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Abone listesi alınamadı: {Status}", (int)response.StatusCode);
                return ([], 0);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<PagedApiResult<SubscriberAdminDto>>(JsonOptions, ct);
            var rows = wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<SubscriberAdminDto>)[];
            return (rows, wrapper?.TotalCount ?? 0);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Abone listesi uç noktasına erişilemedi.");
            return ([], 0);
        }
        catch (TaskCanceledException)
        {
            return ([], 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Abone listesi alınırken beklenmeyen hata oluştu.");
            return ([], 0);
        }
    }

    public async Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/subscriber/admin/counts", paged: false));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Abone sayaçları alınamadı: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<StatusCountsModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Abone sayaç uç noktasına erişilemedi.");
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Abone sayaçları alınırken beklenmeyen hata oluştu.");
            return null;
        }
    }

    public async Task<bool> DeleteAsync(int id, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/subscriber/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Abone silinirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ToggleActiveAsync(int id, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/subscriber/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Abone aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestoreAsync(int id, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/subscriber/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Abone geri yüklenirken hata oluştu: {Id}", id);
            return false;
        }
    }
}