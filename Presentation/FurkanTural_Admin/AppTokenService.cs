namespace FurkanTural_Admin;

public interface IAppTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    void Invalidate(string token);
}

/// <summary>Panelin kendi kimliğini API'ye kanıtlayan uygulama jetonu. Panel kullanıcı adına değil, uygulama adına konuşurken bu jetonu taşır; API tarafında giriş ucu, isteğin bir ön-yüzden mi yoksa doğrudan mı geldiğini yalnızca buradan ayırt edebilir.<para>Anahtar yapılandırılmamışsa veya hâlâ yer tutucuysa API'ye hiç gidilmez ve boş dize döner: jeton alınamadığında panel eskisi gibi çalışmaya devam eder, yalnızca kendini tanıtamaz.</para><para>Jeton süresinden bir saat önce yenilenir, başarısız denemeden sonra kısa bir süre yeniden denenmez. Aksi hâlde API kapalıyken her giriş denemesi ek bir çağrı daha üretirdi.</para></summary>
public class AppTokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AppTokenService> logger)
    : IAppTokenService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AppTokenService> _logger = logger;

    public static readonly TimeSpan FailureBackoff = TimeSpan.FromSeconds(30);

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private DateTime _nextAttempt = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        if (DateTime.UtcNow < _nextAttempt)
            return _cachedToken ?? string.Empty;

        var appKey = _configuration["Api:AppKey"];
        var appName = _configuration["Api:AppName"];

        if (!IsConfigured(appKey) || !IsConfigured(appName))
            return string.Empty;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            if (DateTime.UtcNow < _nextAttempt)
                return _cachedToken ?? string.Empty;

            var client = _httpClientFactory.CreateClient("AppTokenClient");
            var response = await client.PostAsJsonAsync("/api/v1/Auth/app-token",
                new { appKey, appName }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("App token alınamadı. Status: {Status}", response.StatusCode);
                _nextAttempt = DateTime.UtcNow + FailureBackoff;
                return _cachedToken ?? string.Empty;
            }

            var result = await response.Content.ReadFromJsonAsync<AppTokenResponse>(cancellationToken: cancellationToken);

            if (result?.Data?.Token is not null)
            {
                _cachedToken = result.Data.Token;
                _tokenExpiry = result.Data.ExpiresAt.AddHours(-1);
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

    private static bool IsConfigured(string? value)
        => !string.IsNullOrWhiteSpace(value)
        && !value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("####");

    public void Invalidate(string token)
    {
        if (!string.Equals(_cachedToken, token, StringComparison.Ordinal))
            return;

        _cachedToken = null;
        _tokenExpiry = DateTime.MinValue;
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

/// <summary>İstek kendi Authorization başlığını taşımıyorsa uygulama jetonunu ekler. Kullanıcı oturumunun jetonunu asla ezmez: giriş ucu dışındaki çağrılar kendi başlığını kendisi kurar.</summary>
public class AppTokenFallbackHandler(IAppTokenService appTokenService) : DelegatingHandler
{
    private readonly IAppTokenService _appTokenService = appTokenService;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? attached = null;
        if (request.Headers.Authorization is null)
        {
            var token = await _appTokenService.GetTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                attached = token;
            }
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && response.Headers.WwwAuthenticate.Count > 0 && attached is not null)
            _appTokenService.Invalidate(attached);

        return response;
    }
}
