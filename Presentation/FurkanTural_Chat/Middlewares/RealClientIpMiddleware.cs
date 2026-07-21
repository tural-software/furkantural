using System.Net;

namespace FurkanTural_Chat.Middlewares;

/// <summary>
/// Cloudflare + IIS arkasında <c>Connection.RemoteIpAddress</c> ziyaretçiyi değil, Cloudflare
/// edge sunucusunu gösterir. Bu middleware güvenilir bir proxy'den geldiği doğrulanan isteklerde
/// gerçek ziyaretçi IP'sini <c>CF-Connecting-IP</c> (yoksa <c>X-Forwarded-For</c>) başlığından
/// çözer ve <c>RemoteIpAddress</c>'i onunla değiştirir.
///
/// Chat'te neden gerekli: <c>ClientLogController</c> tarayıcının IP'sini gövdeyle API'ye relay
/// eder; düzeltilmezse sistem log tablosuna Cloudflare edge IP'si yazılırdı.
///
/// GÜVENLİK: Başlıklar YALNIZCA istek güvenilir bir proxy ağından geldiyse dikkate alınır.
/// Aksi halde origin'i doğrudan bulan biri CF-Connecting-IP uydurup log tablosunu zehirleyebilirdi.
///
/// ⚠️ İKİZ DOSYA: Aynı mantığın bir kopyası
/// <c>Web/FurkanTural_API/Middlewares/RealClientIpMiddleware.cs</c> içindedir.
/// Birini değiştirirken diğerini de güncelleyin.
/// </summary>
public sealed class RealClientIpMiddleware(RequestDelegate next, IReadOnlyList<IPNetwork> trustedProxies)
{
    private readonly RequestDelegate _next = next;
    private readonly IReadOnlyList<IPNetwork> _trustedProxies = trustedProxies;

    public async Task InvokeAsync(HttpContext context)
    {
        var peer = context.Connection.RemoteIpAddress;
        if (peer is not null && IsTrusted(peer))
        {
            var real = ResolveClientIp(context);
            if (real is not null)
                context.Connection.RemoteIpAddress = real;
        }

        await _next(context);
    }

    private IPAddress? ResolveClientIp(HttpContext context)
    {
        // 1) Cloudflare'in kanonik başlığı: her zaman TEK ve gerçek ziyaretçi IP'si.
        var cf = context.Request.Headers["CF-Connecting-IP"].ToString();
        if (IPAddress.TryParse(cf.Trim(), out var cfIp))
            return cfIp;

        // 2) Yedek: X-Forwarded-For "client, proxy1, proxy2" biçimindedir. Sağdan sola gidip
        //    güvenilir proxy'leri atlıyoruz; ilk güvenilmeyen giriş gerçek istemcidir. (Soldan
        //    almak istemcinin uydurduğu değeri kabul etmek olurdu.)
        var xff = context.Request.Headers["X-Forwarded-For"].ToString();
        if (string.IsNullOrWhiteSpace(xff))
            return null;

        var hops = xff.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = hops.Length - 1; i >= 0; i--)
        {
            if (!TryParseHop(hops[i], out var hop))
                continue;
            if (!IsTrusted(hop))
                return hop;
        }

        return null;
    }

    // XFF girişleri port taşıyabilir ("1.2.3.4:5678" / "[::1]:5678") — IPAddress.TryParse bunu
    // ayrıştıramaz, önce portu ayıklıyoruz.
    private static bool TryParseHop(string value, out IPAddress address)
    {
        if (IPAddress.TryParse(value, out address!))
            return true;

        if (IPEndPoint.TryParse(value, out var endpoint))
        {
            address = endpoint.Address;
            return true;
        }

        address = default!;
        return false;
    }

    private bool IsTrusted(IPAddress address)
    {
        // Cloudflare IPv4 aralıkları IPv6'ya eşlenmiş gelebilir (::ffff:104.16.0.1) — normalize et.
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        // Loopback güvenilir kabul edilir: yerel geliştirme ve (kullanılırsa) out-of-process
        // IIS barındırmasında ANCM isteği loopback üzerinden proxy'ler.
        // Üretimde risk taşımaz çünkü hostingModel="inprocess" — soketi IIS'in kendisi tutar,
        // uygulama ayrı bir localhost portu dinlemez; dolayısıyla aynı makinedeki başka bir
        // süreç buraya loopback'ten bağlanıp başlık uyduramaz.
        if (IPAddress.IsLoopback(address))
            return true;

        foreach (var network in _trustedProxies)
        {
            if (network.BaseAddress.AddressFamily == address.AddressFamily && network.Contains(address))
                return true;
        }

        return false;
    }
}

public static class RealClientIpMiddlewareExtensions
{
    /// <summary>
    /// Güvenilir proxy ağları <c>ForwardedHeaders:TrustedProxies</c> (CIDR dizisi) ile
    /// yapılandırılabilir; boş bırakılırsa Cloudflare'in yayınladığı aralıklar kullanılır.
    /// <c>ForwardedHeaders:Enabled</c> false ise middleware hiç eklenmez.
    /// </summary>
    public static IApplicationBuilder UseRealClientIp(this IApplicationBuilder app, IConfiguration configuration)
    {
        if (!(configuration.GetValue<bool?>("ForwardedHeaders:Enabled") ?? true))
            return app;

        var configured = configuration.GetSection("ForwardedHeaders:TrustedProxies").Get<string[]>();
        var networks = ParseNetworks(configured is { Length: > 0 } ? configured : CloudflareRanges);

        return app.UseMiddleware<RealClientIpMiddleware>(networks);
    }

    private static IReadOnlyList<IPNetwork> ParseNetworks(IEnumerable<string> cidrs)
    {
        var list = new List<IPNetwork>();
        foreach (var cidr in cidrs)
        {
            // Hatalı bir CIDR yüzünden uygulama açılmasın diye sessizce atlıyoruz; gömülü
            // varsayılanlar zaten geçerli, buraya ancak elle yazılmış hatalı config düşer.
            if (IPNetwork.TryParse(cidr, out var network))
                list.Add(network);
        }
        return list;
    }

    // Cloudflare'in yayınladığı origin-facing aralıklar — https://www.cloudflare.com/ips/
    // Nadiren değişir; değiştiğinde appsettings'teki ForwardedHeaders:TrustedProxies ile
    // yeniden yayın yapmadan güncellenebilir.
    private static readonly string[] CloudflareRanges =
    [
        // IPv4
        "173.245.48.0/20",
        "103.21.244.0/22",
        "103.22.200.0/22",
        "103.31.4.0/22",
        "141.101.64.0/18",
        "108.162.192.0/18",
        "190.93.240.0/20",
        "188.114.96.0/20",
        "197.234.240.0/22",
        "198.41.128.0/17",
        "162.158.0.0/15",
        "104.16.0.0/13",
        "104.24.0.0/14",
        "172.64.0.0/13",
        "131.0.72.0/22",
        // IPv6
        "2400:cb00::/32",
        "2606:4700::/32",
        "2803:f800::/32",
        "2405:b500::/32",
        "2405:8100::/32",
        "2a06:98c0::/29",
        "2c0f:f248::/32",
    ];
}
