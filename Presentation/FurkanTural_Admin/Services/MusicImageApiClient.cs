using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Models.MusicImage;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class MusicImageApiClient(HttpClient httpClient, ILogger<MusicImageApiClient> logger) : IMusicImageApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<MusicImageApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions  = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IReadOnlyList<MusicImageAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/musicimage/admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MusicImage listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<MusicImageAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<MusicImageAdminDto>)[];
        }
        catch (HttpRequestException ex) { _logger.LogWarning(ex, "MusicImage listesi uç noktasına erişilemedi."); return []; }
        catch (TaskCanceledException) { return []; }
        catch (Exception ex) { _logger.LogError(ex, "MusicImage listesi alınırken beklenmeyen hata."); return []; }
    }

    public async Task<int?> CreateAsync(IFormFile imageFile, string? altText, bool isCover, int musicId, string token, CancellationToken ct = default)
    {
        try
        {
            using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/musicimage");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var body = new { imageData = bytes, imageName = imageFile.FileName, altText, isCover, musicId };
            request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MusicImage oluşturulamadı: {Status}", (int)response.StatusCode);
                return null;
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<MusicImageIdResult>>(JsonOptions, ct);
            return wrapper?.Data?.Id;
        }
        catch (Exception ex) { _logger.LogError(ex, "MusicImage oluşturulurken hata."); return null; }
    }

    public async Task<bool> UpdateAsync(int id, IFormFile? imageFile, string? altText, bool isCover, int musicId, string token, CancellationToken ct = default)
    {
        try
        {
            byte[]? bytes    = null;
            string? imageName = null;

            if (imageFile is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms, ct);
                bytes     = ms.ToArray();
                imageName = imageFile.FileName;
            }

            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/musicimage");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var body = new { id, imageData = bytes, imageName, altText, isCover, musicId };
            request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "MusicImage güncellenirken hata: {Id}", id); return false; }
    }

    public async Task<MusicImageAdminDto?> GetByIdForAdminAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/musicimage/admin/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<MusicImageAdminDto>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex) { _logger.LogError(ex, "MusicImage {Id} alınırken hata.", id); return null; }
    }

    public async Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/musicimage/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "MusicImage toggle-active hatası: {Id}", id); return false; }
    }

    public async Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/musicimage/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "MusicImage restore hatası: {Id}", id); return false; }
    }

    public async Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/musicimage/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "MusicImage silinirken hata: {Id}", id); return false; }
    }

    private sealed record MusicImageIdResult(int Id);
}
