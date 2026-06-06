using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.ChatMessage;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class ChatMessageApiClient(HttpClient httpClient, ILogger<ChatMessageApiClient> logger) : IChatMessageApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ChatMessageApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<ChatMessageAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            // /message/admin sayfalı döner; admin tarafında client-side filtre/sayfalama için büyük sayfa çekiyoruz.
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/message/admin?pageNumber=1&pageSize=100000");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mesaj listesi alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<ChatMessageAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<ChatMessageAdminDto>)[];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj listesi alınırken hata oluştu.");
            return [];
        }
    }

    public async Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/message/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/message/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj geri yüklenirken hata oluştu: {Id}", id);
            return false;
        }
    }
}
