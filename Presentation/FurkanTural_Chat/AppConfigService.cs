namespace FurkanTural_Chat;

public interface IAppConfigService
{
    Task<string?> GetTurnstileSiteKeyAsync(CancellationToken cancellationToken = default);
}

/// <summary>Bu uygulamaya açılmış yapılandırma değerlerini API'den çeker. Şifre çözme mantığı sunum tarafında durmaz; değerler çözülmüş olarak gelir ve anahtarın kendisi buraya hiç inmez.<para>Sonuç yarım saat önbelleklenir ve önbellek süreç belleğindedir. Hata durumunda istisna fırlatılmaz, elde ne varsa o döner: yapılandırma alınamadı diye sayfa açılmamazlık etmez, ilgili alan yalnızca boş kalır.</para></summary>
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

    /// <summary>Önbellek iki kez denetlenir: biri kilitten önce, biri sonra. Aynı anda gelen istekler aksi hâlde hepsi birden API'ye giderdi; ikinci denetim bekleyenlerin ilkinin getirdiğini kullanmasını sağlar.<para>Adlandırılmış istemci uygulama jetonunu kendi ekler; bu uç yalnızca o jetonla açıktır.</para></summary>
    private async Task<Dictionary<string, string?>?> GetConfigAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
            return _cache;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
                return _cache;

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
