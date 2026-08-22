using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Chat.Models.Auth;
using FurkanTural_Chat.Models.Wrappers;

namespace FurkanTural_Chat.Services;

public class ChatAuthApiClient(HttpClient httpClient, ILogger<ChatAuthApiClient> logger) : IChatAuthApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ChatAuthApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<ApiResult<AuthResultModel>> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken = default)
        => PostAsync("/api/v1/Auth/login", new
        {
            username = request.Username,
            password = request.Password,
            turnstileToken = request.TurnstileToken,
            appSource = "Chat"
        }, cancellationToken);

    public Task<ApiResult<AuthResultModel>> RegisterAsync(RegisterRequestModel request, CancellationToken cancellationToken = default)
        => PostAsync("/api/v1/Auth/register", new
        {
            username = request.Username,
            email = request.Email,
            password = request.Password,
            displayName = request.DisplayName,
            turnstileToken = request.TurnstileToken,
            acceptAgreement = request.AcceptAgreement
        }, cancellationToken);

    private async Task<ApiResult<AuthResultModel>> PostAsync(string url, object body, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResult<AuthResultModel>>(JsonOptions, cancellationToken);
            return result ?? ApiResult<AuthResultModel>.Fail("API'den boş yanıt alındı.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API erişim hatası.");
            return ApiResult<AuthResultModel>.Fail("API'ye erişilemedi. Lütfen sunucunun çalıştığından emin olun.", 503);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<AuthResultModel>.Fail("İstek zaman aşımına uğradı.", 504);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kimlik doğrulama isteği başarısız.");
            return ApiResult<AuthResultModel>.Fail("Beklenmeyen bir hata oluştu.", 500);
        }
    }
}