namespace FurkanTural_Chat;

public interface IAppConfigService
{
    Task<string?> GetTurnstileSiteKeyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// API'nin app-config ucundan (yalnızca app-token ile erişilebilir) bu uygulamaya
/// izin verilen, çözülmüş config değerlerini çeker ve önbelleğe alır. Şifre çözme
/// mantığı ön-yüzde tutulmaz; değerler API'den hazır (çözülmüş) gelir.
/// </summary>
public class AppConfigService : IAppConfigService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppConfigService> _logger;

    private Dictionary<string, string?>? _cache;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AppConfigService(IHttpClientFactory httpClientFactory, ILogger<AppConfigService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> GetTurnstileSiteKeyAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigAsync(cancellationToken);
        return config is not null && config.TryGetValue("Turnstile:SiteKey", out var value) ? value : null;
    }

    private async Task<Dictionary<string, string?>?> GetConfigAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
            return _cache;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
                return _cache;

            // "ApiClient" → DefaultTokenHandler app-token'ı otomatik ekler.
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync("/api/v1/config/app", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("App config alınamadı. Status: {Status}", response.StatusCode);
                return _cache;
            }

            var result = await response.Content.ReadFromJsonAsync<AppConfigResponse>(cancellationToken: cancellationToken);
            if (result?.Data is not null)
            {
                _cache = result.Data;
                _cacheExpiry = DateTime.UtcNow.AddMinutes(30);
            }

            return _cache;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "App config alınırken hata oluştu.");
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    private class AppConfigResponse
    {
        public Dictionary<string, string?>? Data { get; set; }
    }
}
