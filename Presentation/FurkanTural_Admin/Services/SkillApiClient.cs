using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Skill;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class SkillApiClient(HttpClient httpClient, ILogger<SkillApiClient> logger) : ISkillApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<SkillApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<SkillAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/skill/admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Beceri listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<SkillAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<SkillAdminDto>)[];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Beceri listesi uç noktasına erişilemedi.");
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beceri listesi alınırken beklenmeyen hata oluştu.");
            return [];
        }
    }

    public async Task<(IReadOnlyList<SkillAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/skill/admin/paged", paged: true));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Beceri listesi alınamadı: {Status}", (int)response.StatusCode);
                return ([], 0);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<PagedApiResult<SkillAdminDto>>(JsonOptions, ct);
            var rows = wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<SkillAdminDto>)[];
            return (rows, wrapper?.TotalCount ?? 0);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Beceri listesi uç noktasına erişilemedi.");
            return ([], 0);
        }
        catch (TaskCanceledException)
        {
            return ([], 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beceri listesi alınırken beklenmeyen hata oluştu.");
            return ([], 0);
        }
    }

    public async Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/skill/admin/counts", paged: false));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Beceri sayaçları alınamadı: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<StatusCountsModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Beceri sayaç uç noktasına erişilemedi.");
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beceri sayaçları alınırken beklenmeyen hata oluştu.");
            return null;
        }
    }

    public async Task<ApiCallResult> CreateAsync(SkillFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/skill");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(dto, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beceri oluşturulurken hata oluştu.");
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> UpdateAsync(int id, SkillFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/skill");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var body = new { id, name = dto.Name, proficiency = dto.Proficiency };
            request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beceri güncellenirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/skill/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beceri silinirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/skill/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beceri aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/skill/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beceri geri yüklenirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }
}