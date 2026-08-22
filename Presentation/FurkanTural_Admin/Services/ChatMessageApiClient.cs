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
            // Uç sayfalı döner ama panel filtreleme ve sayfalamayı istemcide yaptığı için tek seferde
            // hepsi isteniyor. Bu geçici bir çözümdür: mesaj sayısı büyüdükçe hem bellek hem süre
            // maliyeti doğrusal artar ve bir noktada sayfalama sunucuya taşınmak zorunda kalacak.
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

    public async Task<(Stream? Stream, string? ContentType)> GetAttachmentAsync(string file, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/message/attachment?file=" + Uri.EscapeDataString(file));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // Yanıt bilerek serbest bırakılmaz; akış tüketildiğinde bağlantı havuza kendiliğinden döner.
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                _logger.LogWarning("Ek dosyası alınamadı: {File}, Status: {Status}", file, (int)response.StatusCode);
                return (null, null);
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            return (stream, response.Content.Headers.ContentType?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ek dosyası alınırken hata oluştu: {File}", file);
            return (null, null);
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