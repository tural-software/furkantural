using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class AdminDashboardClient(HttpClient httpClient, ILogger<AdminDashboardClient> logger) : IAdminDashboardClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AdminDashboardClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AdminDashboardModel?> GetAsync(int windowDays, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/dashboard/admin/summary?windowDays={windowDays}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pano özeti alınamadı: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<AdminDashboardModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Pano özeti uç noktasına erişilemedi.");
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }
}
