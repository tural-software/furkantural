using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class AdminSummaryClient(HttpClient httpClient, ILogger<AdminSummaryClient> logger) : IAdminSummaryClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AdminSummaryClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<EntitySummaryModel?> GetAsync(string entityPath, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/{entityPath}/admin/summary");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Özet alınamadı: {Path} → {Status}", entityPath, (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<EntitySummaryModel>>(JsonOptions, cancellationToken);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Özet uç noktasına erişilemedi: {Path}", entityPath);
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen özet hatası: {Path}", entityPath);
            return null;
        }
    }
}