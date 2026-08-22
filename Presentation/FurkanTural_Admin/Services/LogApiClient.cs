using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Log;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class LogApiClient(HttpClient httpClient, ILogger<LogApiClient> logger) : ILogApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<LogApiClient> _logger = logger;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<(IReadOnlyList<LogAdminDto> Rows, int TotalCount)> GetAdminPagedAsync(
        string? level, string? project, string? message,
        DateTime? dateFrom, DateTime? dateTo,
        int pageNumber, int pageSize,
        string token, CancellationToken ct = default)
    {
        try
        {
            var qs = new StringBuilder("/api/v1/log/admin/paged?");
            qs.Append($"pageNumber={pageNumber}&pageSize={pageSize}");

            if (!string.IsNullOrWhiteSpace(level))
                qs.Append($"&level={Uri.EscapeDataString(level)}");
            if (!string.IsNullOrWhiteSpace(project))
                qs.Append($"&project={Uri.EscapeDataString(project)}");
            if (!string.IsNullOrWhiteSpace(message))
                qs.Append($"&message={Uri.EscapeDataString(message)}");
            if (dateFrom.HasValue)
                qs.Append($"&dateFrom={Uri.EscapeDataString(dateFrom.Value.ToString("yyyy-MM-dd"))}");
            if (dateTo.HasValue)
                qs.Append($"&dateTo={Uri.EscapeDataString(dateTo.Value.ToString("yyyy-MM-dd"))}");

            using var request = new HttpRequestMessage(HttpMethod.Get, qs.ToString());
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Log listesi alınamadı: {Status}", (int)response.StatusCode);
                return ([], 0);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<PagedApiResult<LogAdminDto>>(JsonOptions, ct);
            var rows = wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<LogAdminDto>)[];
            var total = wrapper?.TotalCount ?? 0;
            return (rows, total);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Log listesi uç noktasına erişilemedi.");
            return ([], 0);
        }
        catch (TaskCanceledException)
        {
            return ([], 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log listesi alınırken beklenmeyen hata oluştu.");
            return ([], 0);
        }
    }

    public async Task<EntitySummaryModel?> GetAdminSummaryAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/log/admin/summary");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<EntitySummaryModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log özeti alınırken hata oluştu.");
            return null;
        }
    }
}