namespace FurkanTural_Chat;

public interface IAppTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}

public class AppTokenService : IAppTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppTokenService> _logger;

    public static readonly TimeSpan FailureBackoff = TimeSpan.FromSeconds(30);

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private DateTime _nextAttempt = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AppTokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AppTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        if (DateTime.UtcNow < _nextAttempt)
            return _cachedToken ?? string.Empty;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            if (DateTime.UtcNow < _nextAttempt)
                return _cachedToken ?? string.Empty;

            var appKey = _configuration["Api:AppKey"];
            var appName = _configuration["Api:AppName"];

            var client = _httpClientFactory.CreateClient("AppTokenClient");
            var response = await client.PostAsJsonAsync("/api/v1/Auth/app-token",
                new { appKey, appName }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("App token alınamadı. Status: {Status}", response.StatusCode);
                _nextAttempt = DateTime.UtcNow + FailureBackoff;
                return _cachedToken ?? string.Empty;
            }

            var result = await response.Content.ReadFromJsonAsync<AppTokenResponse>(
                cancellationToken: cancellationToken);

            if (result?.Data?.Token is not null)
            {
                _cachedToken = result.Data.Token;
                _tokenExpiry = result.Data.ExpiresAt.AddHours(-1); // 1 saat erken yenile
                _nextAttempt = DateTime.MinValue;
            }

            return _cachedToken ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "App token alınırken hata oluştu.");
            _nextAttempt = DateTime.UtcNow + FailureBackoff;
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