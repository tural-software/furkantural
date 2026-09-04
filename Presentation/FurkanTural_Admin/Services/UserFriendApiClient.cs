using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Helpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.UserFriend;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class UserFriendApiClient(HttpClient httpClient, ILogger<UserFriendApiClient> logger) : IUserFriendApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<UserFriendApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<UserFriendAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/friend/admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Arkadaşlık listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<UserFriendAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<UserFriendAdminDto>)[];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arkadaşlık listesi alınırken hata oluştu.");
            return [];
        }
    }

    public async Task<(IReadOnlyList<UserFriendAdminDto> Rows, int TotalFiltered)> GetAdminPagedAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/friend/admin/paged", paged: true));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Arkadaşlık listesi alınamadı: {Status}", (int)response.StatusCode);
                return ([], 0);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<PagedApiResult<UserFriendAdminDto>>(JsonOptions, ct);
            var rows = wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<UserFriendAdminDto>)[];
            return (rows, wrapper?.TotalCount ?? 0);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Arkadaşlık listesi uç noktasına erişilemedi.");
            return ([], 0);
        }
        catch (TaskCanceledException)
        {
            return ([], 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arkadaşlık listesi alınırken beklenmeyen hata oluştu.");
            return ([], 0);
        }
    }

    public async Task<StatusCountsModel?> GetAdminCountsAsync(AdminListRequest request, string token, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ToQueryString("/api/v1/friend/admin/counts", paged: false));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Arkadaşlık sayaçları alınamadı: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<StatusCountsModel>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Arkadaşlık sayaç uç noktasına erişilemedi.");
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arkadaşlık sayaçları alınırken beklenmeyen hata oluştu.");
            return null;
        }
    }

    public async Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/friend/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arkadaşlık aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/friend/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arkadaşlık geri yüklenirken hata oluştu: {Id}", id);
            return false;
        }
    }
}