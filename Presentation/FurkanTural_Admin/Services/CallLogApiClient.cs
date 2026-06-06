using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.Call;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class CallLogApiClient(HttpClient httpClient, ILogger<CallLogApiClient> logger) : ICallLogApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CallLogApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<CallLogAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            // /call/admin sayfalı döner; admin client-side filtre/sayfalama için büyük sayfa çekiyoruz.
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/call/admin?pageNumber=1&pageSize=100000");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Arama listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<CallLogAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<CallLogAdminDto>)[];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arama listesi alınırken hata oluştu.");
            return [];
        }
    }

    public async Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/call/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arama aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/call/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arama kaydı geri yüklenirken hata oluştu: {Id}", id);
            return false;
        }
    }
}
