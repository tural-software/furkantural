using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.Schema;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class SchemaApiClient(HttpClient httpClient, ILogger<SchemaApiClient> logger) : ISchemaApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<SchemaApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TableSchemaModel?> GetAsync(string entity, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/schema/{Uri.EscapeDataString(entity)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Şema alınamadı: {Entity} → {Status}", entity, (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<TableSchemaModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Şema uç noktasına erişilemedi: {Entity}", entity);
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen şema hatası: {Entity}", entity);
            return null;
        }
    }
}
