using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.DataRetention;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public interface IDataRetentionApiClient
{
    Task<DataRetentionReportModel?> PreviewAsync(string token, CancellationToken ct = default);
    Task<DataRetentionReportModel?> PurgeAsync(string token, CancellationToken ct = default);
}

public class DataRetentionApiClient(HttpClient httpClient, ILogger<DataRetentionApiClient> logger) : IDataRetentionApiClient
{
    private const string Base = "/api/v1/dataretention";

    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<DataRetentionApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<DataRetentionReportModel?> PreviewAsync(string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Get, $"{Base}/admin/preview", token, ct);

    public Task<DataRetentionReportModel?> PurgeAsync(string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Post, $"{Base}/admin/purge", token, ct);

    private async Task<DataRetentionReportModel?> SendAsync(HttpMethod method, string url, string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Saklama temizliği isteği başarısız: {Method} {Status}", method, (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<DataRetentionReportModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saklama temizliği isteği sırasında hata oluştu: {Method}", method);
            return null;
        }
    }
}
