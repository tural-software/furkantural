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
        }, ApiResult<AuthResultModel>.Fail, cancellationToken);

    public Task<ApiResult<AuthResultModel>> RegisterAsync(RegisterRequestModel request, CancellationToken cancellationToken = default)
        => PostAsync("/api/v1/Auth/register", new
        {
            username = request.Username,
            email = request.Email,
            password = request.Password,
            displayName = request.DisplayName,
            turnstileToken = request.TurnstileToken,
            acceptAgreement = request.AcceptAgreement
        }, ApiResult<AuthResultModel>.Fail, cancellationToken);

    public Task<ApiResult> ActivateAsync(string? token, CancellationToken cancellationToken = default)
        => PostAsync("/api/v1/Auth/activate", new { token }, ApiResult.Fail, cancellationToken);

    private async Task<TResult> PostAsync<TResult>(string url, object body, Func<string, int, TResult> fail, CancellationToken cancellationToken)
        where TResult : ApiResult
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, cancellationToken);
            return result ?? fail("API'den boş yanıt alındı.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API erişim hatası.");
            return fail("API'ye erişilemedi. Lütfen sunucunun çalıştığından emin olun.", 503);
        }
        catch (TaskCanceledException)
        {
            return fail("İstek zaman aşımına uğradı.", 504);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kimlik doğrulama isteği başarısız.");
            return fail("Beklenmeyen bir hata oluştu.", 500);
        }
    }
}
