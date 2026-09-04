using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Models.Report;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class ReportApiClient(HttpClient httpClient, ILogger<ReportApiClient> logger) : IReportApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ReportApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<ReportAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/report/admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Şikayet listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<ReportAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<ReportAdminDto>)[];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şikayet listesi alınırken hata oluştu.");
            return [];
        }
    }

    public async Task<(IReadOnlyList<ReportAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/report/admin/paged", paged: true));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Şikayet listesi alınamadı: {Status}", (int)response.StatusCode);
                return ([], 0);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<PagedApiResult<ReportAdminDto>>(JsonOptions, ct);
            var rows = wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<ReportAdminDto>)[];
            return (rows, wrapper?.TotalCount ?? 0);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Şikayet listesi uç noktasına erişilemedi.");
            return ([], 0);
        }
        catch (TaskCanceledException)
        {
            return ([], 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şikayet listesi alınırken beklenmeyen hata oluştu.");
            return ([], 0);
        }
    }

    public async Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/report/admin/counts", paged: false));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Şikayet sayaçları alınamadı: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<StatusCountsModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Şikayet sayaç uç noktasına erişilemedi.");
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şikayet sayaçları alınırken beklenmeyen hata oluştu.");
            return null;
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, string? adminNote, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/report/{id}/status");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var body = JsonSerializer.Serialize(new { status, adminNote });
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şikayet durumu güncellenirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/report/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şikayet aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/report/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şikayet geri yüklenirken hata oluştu: {Id}", id);
            return false;
        }
    }
}