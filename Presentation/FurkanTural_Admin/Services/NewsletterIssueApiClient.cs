using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.NewsletterIssue;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class NewsletterIssueApiClient(HttpClient httpClient, ILogger<NewsletterIssueApiClient> logger) : INewsletterIssueApiClient
{
    private const string Base = "/api/v1/newsletterissue";

    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<NewsletterIssueApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<(IReadOnlyList<NewsletterIssueAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = Get(request.ToQueryString($"{Base}/admin/paged", paged: true), token);
            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bülten listesi alınamadı: {Status}", (int)response.StatusCode);
                return ([], 0);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<PagedApiResult<NewsletterIssueAdminDto>>(JsonOptions, ct);
            var rows = wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<NewsletterIssueAdminDto>)[];
            return (rows, wrapper?.TotalCount ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bülten listesi alınırken hata oluştu.");
            return ([], 0);
        }
    }

    public async Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = Get(request.ToQueryString($"{Base}/admin/counts", paged: false), token);
            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode) return null;

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<StatusCountsModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bülten sayaçları alınırken hata oluştu.");
            return null;
        }
    }

    public async Task<int> GetAudienceCountAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = Get($"{Base}/admin/audience", token);
            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode) return 0;

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<int>>(JsonOptions, ct);
            return wrapper?.Data ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doğrulanmış abone sayısı alınırken hata oluştu.");
            return 0;
        }
    }

    public async Task<NewsletterIssueProgressModel?> GetProgressAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = Get($"{Base}/{id}/progress", token);
            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode) return null;

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<NewsletterIssueProgressModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bülten ilerlemesi alınırken hata oluştu: {Id}", id);
            return null;
        }
    }

    public Task<ApiCallResult> CreateAsync(NewsletterIssueFormDto dto, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Post, Base, token, dto, "Bülten oluşturulurken hata oluştu.", ct);

    public Task<ApiCallResult> UpdateAsync(int id, NewsletterIssueFormDto dto, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Put, $"{Base}/{id}", token, dto, "Bülten güncellenirken hata oluştu.", ct);

    public Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"{Base}/{id}", token, null, "Bülten silinirken hata oluştu.", ct);

    public Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Patch, $"{Base}/{id}/toggle-active", token, null, "Bülten durumu değiştirilirken hata oluştu.", ct);

    public Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Patch, $"{Base}/{id}/restore", token, null, "Bülten geri yüklenirken hata oluştu.", ct);

    public Task<ApiCallResult> QueueAsync(int id, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Post, $"{Base}/{id}/queue", token, new { }, "Bülten dağıtıma verilirken hata oluştu.", ct);

    public Task<ApiCallResult> SendTestAsync(int id, string? email, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Post, $"{Base}/{id}/test", token, new { email }, "Deneme gönderilirken hata oluştu.", ct);

    public async Task<BulkResultModel?> BulkAsync(string action, IReadOnlyList<int> ids, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{Base}/admin/bulk")
            {
                Content = JsonContent.Create(new { ids, action })
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bülten toplu işlemi başarısız: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<BulkResultModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bülten toplu işlemi sırasında hata oluştu.");
            return null;
        }
    }

    private static HttpRequestMessage Get(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<ApiCallResult> SendAsync(HttpMethod method, string url, string token, object? body, string logMessage, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null)
                request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", logMessage);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }
}
