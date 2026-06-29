using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.User;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger) : IUserApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<UserApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<UserAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user/admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kullanıcı listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<UserAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<UserAdminDto>)[];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Kullanıcı listesi uç noktasına erişilemedi.");
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı listesi alınırken beklenmeyen hata oluştu.");
            return [];
        }
    }

    public async Task<ApiCallResult> CreateAsync(UserFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var body = new { username = dto.Username, password = dto.Password, roleId = dto.RoleId, email = dto.Email, displayName = dto.DisplayName };
            request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı oluşturulurken hata oluştu.");
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> UpdateAsync(int id, UserFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var body = new { id, username = dto.Username, password = dto.Password, roleId = dto.RoleId, email = dto.Email, displayName = dto.DisplayName };
            request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> UploadAvatarAsync(int id, IFormFile file, string token, CancellationToken ct = default)
    {
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();

            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/user/{id}/avatar");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var body = new { imageData = bytes, imageName = file.FileName };
            request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Avatar yüklenirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> DeleteAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/user/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı silinirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/user/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }

    public async Task<ApiCallResult> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/user/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            return await response.ToApiCallResultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı geri yüklenirken hata oluştu: {Id}", id);
            return ApiCallResult.Fail(0, "API'ye ulaşılamadı.");
        }
    }
}
