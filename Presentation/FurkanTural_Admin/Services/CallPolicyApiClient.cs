using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Admin.Models.Call;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class CallPolicyApiClient(HttpClient httpClient, ILogger<CallPolicyApiClient> logger) : ICallPolicyApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CallPolicyApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<CallPolicyFormDto?> GetAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/call/policy");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Arama politikası alınamadı: {Status}", (int)response.StatusCode);
                return null;
            }
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResult<CallPolicyFormDto>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arama politikası alınırken hata oluştu.");
            return null;
        }
    }

    public async Task<bool> UpdateAsync(CallPolicyFormDto dto, string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/call/policy");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arama politikası güncellenirken hata oluştu.");
            return false;
        }
    }
}
