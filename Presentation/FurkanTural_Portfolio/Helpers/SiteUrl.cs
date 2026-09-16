namespace FurkanTural_Portfolio.Helpers;

/// <summary>Sayfaya ve SEO dosyalarına yazılan mutlak adreslerin kökü. İsteğin Host başlığı istemcinin yazdığı bir değerdir; olduğu gibi kullanılırsa sahte bir Host ile istenen sitemap, başkasının alan adını gösteren bağlantılarla üretilir ve herkese açık önbelleğe o hâliyle girebilir.<para>Bu yüzden yalnızca bilinen alan adları kabul edilir; tanınmayan her Host için kanonik adres kullanılır. Yerel geliştirme adresleri istisnadır ve olduğu gibi geçer, aksi hâlde yerelde üretilen her bağlantı canlı siteye giderdi. Canlıda şema her zaman https'tir: Cloudflare arkasında bağlantı şeması sunucuya http olarak ulaşabilir.</para></summary>
public static class SiteUrl
{
    public const string Canonical = "https://furkantural.com";

    private static readonly HashSet<string> KnownHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "furkantural.com",
        "www.furkantural.com"
    };

    public static string Base(string scheme, string host, int? port)
    {
        if (IsLocal(host))
            return port is null ? $"{scheme}://{host}" : $"{scheme}://{host}:{port}";

        return KnownHosts.Contains(host) && port is null ? $"https://{host.ToLowerInvariant()}" : Canonical;
    }

    private static bool IsLocal(string host)
        => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || host is "127.0.0.1" or "::1" or "[::1]";
}
