using System.Net;

namespace FurkanTural_Chat.Middlewares;

/// <summary>Cloudflare ve IIS arkasında <c>Connection.RemoteIpAddress</c> ziyaretçiyi değil edge sunucusunu gösterir. Bu middleware gerçek IP'yi <c>CF-Connecting-IP</c>, yoksa <c>X-Forwarded-For</c> başlığından çözüp onun yerine koyar.<para>Chat'in buna ihtiyacı istemci günlüklerinden gelir: tarayıcının IP'si gövdeyle API'ye taşınır ve burada düzeltilmezse günlük tablosuna edge sunucusunun adresi yazılır.</para><para>Başlıklar yalnızca istek güvenilir bir proxy ağından geldiyse dikkate alınır. Bu kapı olmasa origin'i doğrudan bulan biri kendi IP'sini uydurup günlük tablosunu zehirleyebilirdi.</para><para>X-Forwarded-For sağdan sola yürünür: güvenilir proxy'ler atlanır, ilk güvenilmeyen giriş gerçek istemci sayılır. Soldan almak, istemcinin kendi yazdığı değeri kabul etmek olurdu.</para><para>Loopback güvenilir kabul edilir; yerel geliştirme ve süreç dışı IIS barındırması bunu gerektirir. Üretimde risk taşımaz çünkü barındırma süreç içidir: soketi IIS tutar, uygulama ayrı bir localhost portu dinlemez, dolayısıyla aynı makinedeki başka bir süreç buraya bağlanıp başlık uyduramaz.</para><para>Aynı mantığın bir kopyası <c>Web/FurkanTural_API/Middlewares/RealClientIpMiddleware.cs</c> içindedir. Biri değişirse diğeri de değişmelidir.</para></summary>
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
        var cf = context.Request.Headers["CF-Connecting-IP"].ToString();
        if (IPAddress.TryParse(cf.Trim(), out var cfIp))
            return cfIp;

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

    /// <summary>X-Forwarded-For girişleri port taşıyabilir (<c>1.2.3.4:5678</c>, <c>[::1]:5678</c>) ve düz ayrıştırma bunu reddeder; o yüzden uç nokta olarak ikinci bir deneme yapılır.</summary>
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

    /// <summary>Cloudflare adresleri IPv6'ya eşlenmiş biçimde gelebildiği için (<c>::ffff:104.16.0.1</c>) karşılaştırmadan önce normalleştirilir; aksi hâlde aralık eşleşmesi kaçardı.</summary>
    private bool IsTrusted(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

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
    /// <summary>Güvenilir proxy ağları <c>ForwardedHeaders:TrustedProxies</c> (CIDR dizisi) ile yapılandırılabilir; boş bırakılırsa koda gömülü Cloudflare aralıkları kullanılır. <c>ForwardedHeaders:Enabled</c> false ise middleware hiç eklenmez ve istekler edge sunucusunun IP'siyle kaydedilir.</summary>
    public static IApplicationBuilder UseRealClientIp(this IApplicationBuilder app, IConfiguration configuration)
    {
        if (!(configuration.GetValue<bool?>("ForwardedHeaders:Enabled") ?? true))
            return app;

        var configured = configuration.GetSection("ForwardedHeaders:TrustedProxies").Get<string[]>();
        var networks = ParseNetworks(configured is { Length: > 0 } ? configured : CloudflareRanges);

        return app.UseMiddleware<RealClientIpMiddleware>(networks);
    }

    /// <summary>Ayrıştırılamayan CIDR sessizce atlanır: hatalı yazılmış tek bir ayar uygulamayı açılmaktan alıkoymamalıdır. Karşılığında yanlış yazılan aralık hiç uyarı vermeden güvenilmez kalır.</summary>
    private static IReadOnlyList<IPNetwork> ParseNetworks(IEnumerable<string> cidrs)
    {
        var list = new List<IPNetwork>();
        foreach (var cidr in cidrs)
        {
            if (IPNetwork.TryParse(cidr, out var network))
                list.Add(network);
        }
        return list;
    }

    /// <summary>Cloudflare'in yayımladığı origin tarafı aralıklar (<c>https://www.cloudflare.com/ips/</c>). Nadiren değişir ve değiştiğinde yeniden yayın gerekmez: yapılandırmadaki TrustedProxies listesi bu diziyi tümüyle devre dışı bırakır.</summary>
    private static readonly string[] CloudflareRanges =
    [
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
        "2400:cb00::/32",
        "2606:4700::/32",
        "2803:f800::/32",
        "2405:b500::/32",
        "2405:8100::/32",
        "2a06:98c0::/29",
        "2c0f:f248::/32",
    ];
}
