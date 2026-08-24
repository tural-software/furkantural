using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Models.MailTemplate;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class MailTemplateApiClient(HttpClient httpClient, ILogger<MailTemplateApiClient> logger) : IMailTemplateApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<MailTemplateApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<MailTemplateAdminDto>> GetAllForAdminAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/mailtemplate/admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("İletişim şablonları alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<MailTemplateAdminDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<MailTemplateAdminDto>)[];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Şablon uç noktasına erişilemedi.");
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İletişim şablonları alınırken beklenmeyen hata oluştu.");
            return [];
        }
    }

    public async Task<IReadOnlyList<MailTemplateTypeOptionDto>> GetTypesAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/mailtemplatetype");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Posta türleri alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<MailTemplateTypeOptionDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<MailTemplateTypeOptionDto>)[];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Posta türleri alınırken hata oluştu.");
            return [];
        }
    }


    public async Task<IReadOnlyList<AppSourceOptionDto>> GetAppSourcesAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/appsource");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Projeler alınamadı: {Status}", (int)response.StatusCode);
                return [];
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<IEnumerable<AppSourceOptionDto>>>(JsonOptions, ct);
            return wrapper?.Data?.ToList().AsReadOnly() ?? (IReadOnlyList<AppSourceOptionDto>)[];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Projeler alınırken hata oluştu.");
            return [];
        }
    }

    public async Task<bool> CreateAsync(MailTemplateFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mailtemplate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(dto, WriteOptions), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şablon oluşturulurken hata oluştu.");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(int id, MailTemplateFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/mailtemplate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var body = new { id, mailTemplateTypeId = dto.MailTemplateTypeId, name = dto.Name, subject = dto.Subject, fileName = dto.FileName, htmlContent = dto.HtmlContent };
            request.Content = new StringContent(JsonSerializer.Serialize(body, WriteOptions), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şablon güncellenirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/mailtemplate/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şablon silinirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ToggleActiveAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/mailtemplate/{id}/toggle-active");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şablon aktiflik durumu değiştirilirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestoreAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/mailtemplate/{id}/restore");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şablon geri yüklenirken hata oluştu: {Id}", id);
            return false;
        }
    }

    public async Task<string?> GetHtmlContentAsync(int id, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/mailtemplate/admin/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<MailTemplateHtmlResponse>>(JsonOptions, ct);
            return wrapper?.Data?.HtmlContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şablon HTML içeriği alınırken hata oluştu: {Id}", id);
            return null;
        }
    }

    private sealed record MailTemplateHtmlResponse(string? HtmlContent);
}