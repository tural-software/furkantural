using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Comment;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class CommentApiClient(HttpClient httpClient, ILogger<CommentApiClient> logger) : ICommentApiClient
{
    private const string Base = "/api/v1/comment";

    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CommentApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<(IReadOnlyList<CommentAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = Get(request.ToQueryString($"{Base}/admin/paged", paged: true), token);
            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yorum listesi alınamadı: {Status}", (int)response.StatusCode);
                return ([], 0);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<PagedApiResult<CommentAdminDto>>(JsonOptions, ct);
            var rows = wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<CommentAdminDto>)[];
            return (rows, wrapper?.TotalCount ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yorum listesi alınırken hata oluştu.");
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
            _logger.LogError(ex, "Yorum sayaçları alınırken hata oluştu.");
            return null;
        }
    }

    public async Task<CommentModerationCounts?> GetModerationCountsAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = Get($"{Base}/admin/moderation-counts", token);
            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode) return null;

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<CommentModerationCounts>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yorum denetim sayaçları alınırken hata oluştu.");
            return null;
        }
    }

    public Task<ApiCallResult> SetStatusAsync(int id, string status, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Patch, $"{Base}/{id}/status", token, new { status }, "Yorum durumu değiştirilirken hata oluştu.", ct);

    public Task<ApiCallResult> ReplyAsync(int id, CommentReplyFormDto dto, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Post, $"{Base}/{id}/reply", token, new { body = dto.Body }, "Yoruma yanıt verilirken hata oluştu.", ct);

    public Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"{Base}/{id}", token, null, "Yorum silinirken hata oluştu.", ct);

    public Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Patch, $"{Base}/{id}/toggle-active", token, null, "Yorum durumu değiştirilirken hata oluştu.", ct);

    public Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default)
        => SendAsync(HttpMethod.Patch, $"{Base}/{id}/restore", token, null, "Yorum geri yüklenirken hata oluştu.", ct);

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
                _logger.LogWarning("Yorum toplu işlemi başarısız: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<BulkResultModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yorum toplu işlemi sırasında hata oluştu.");
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
