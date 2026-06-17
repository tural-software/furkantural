namespace FurkanTural_Blog;

public interface IAppTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}

public class AppTokenService : IAppTokenService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppTokenService> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    // Token alımı başarısız olduğunda, ölü uca her istek için tekrar tekrar gitmemek
    // için kısa süre yeniden denemeyi durdururuz (negatif önbellek).
    private DateTime _retryBlockedUntil = DateTime.MinValue;
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromSeconds(15);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AppTokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AppTokenService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AppTokenClient");
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        // Yakın zamanda başarısız olduysak, backoff süresi dolana kadar yeniden deneme.
        if (DateTime.UtcNow < _retryBlockedUntil)
            return _cachedToken ?? string.Empty;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            if (DateTime.UtcNow < _retryBlockedUntil)
                return _cachedToken ?? string.Empty;

            var appKey = _configuration["Api:AppKey"];
            var appName = _configuration["Api:AppName"];

            var response = await _httpClient.PostAsJsonAsync("/api/v1/Auth/app-token",
                new { appKey, appName }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("App token alınamadı. Status: {Status}", response.StatusCode);
                _retryBlockedUntil = DateTime.UtcNow + FailureBackoff;
                return _cachedToken ?? string.Empty;
            }

            var result = await response.Content.ReadFromJsonAsync<AppTokenResponse>(
                cancellationToken: cancellationToken);

            if (result?.Data?.Token is not null)
            {
                _cachedToken = result.Data.Token;
                _tokenExpiry = result.Data.ExpiresAt.AddHours(-1); // 1 saat erken yenile
            }

            return _cachedToken ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "App token alınırken hata oluştu.");
            _retryBlockedUntil = DateTime.UtcNow + FailureBackoff;
            return _cachedToken ?? string.Empty;
        }
        finally
        {
            _lock.Release();
        }
    }

    private class AppTokenResponse
    {
        public TokenData? Data { get; set; }
    }

    private class TokenData
    {
        public string? Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}

public class DefaultTokenHandler(IAppTokenService appTokenService) : DelegatingHandler
{
    private readonly IAppTokenService _appTokenService = appTokenService;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _appTokenService.GetTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
