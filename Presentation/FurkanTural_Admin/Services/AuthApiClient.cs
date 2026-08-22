using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Admin.Models.Auth;
using FurkanTural_Admin.Models.Wrappers;

namespace FurkanTural_Admin.Services;

public class AuthApiClient(HttpClient httpClient, ILogger<AuthApiClient> logger) : IAuthApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AuthApiClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ApiResult<LoginResultModel>> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/v1/Auth/login", new
            {
                username = request.Username,
                password = request.Password
            }, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<ApiResult<LoginResultModel>>(JsonOptions, cancellationToken);
            if (result is null)
                return ApiResult<LoginResultModel>.Fail("API'den boş yanıt alındı.", (int)response.StatusCode);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API erişim hatası.");
            return ApiResult<LoginResultModel>.Fail("API'ye erişilemedi. Lütfen sunucunun çalıştığından emin olun.", 503);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<LoginResultModel>.Fail("İstek zaman aşımına uğradı.", 504);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login isteği başarısız.");
            return ApiResult<LoginResultModel>.Fail("Beklenmeyen bir hata oluştu.", 500);
        }
    }
}