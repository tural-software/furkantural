using System.Net.Http.Headers;
using System.Security.Cryptography;
using FurkanTural_Chat;
using FurkanTural_Chat.Middlewares;
using FurkanTural_Chat.Models.Common;
using FurkanTural_Chat.Services;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl yapılandırılmamış.");

builder.Services.AddHttpClient<IChatAuthApiClient, ChatAuthApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// App-token altyapısı — SiteKey'i API'nin app-config ucundan çekmek için
builder.Services.AddHttpClient("AppTokenClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddSingleton<IAppTokenService, AppTokenService>();
builder.Services.AddSingleton<IAppConfigService, AppConfigService>();
builder.Services.AddTransient<DefaultTokenHandler>();
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
}).AddHttpMessageHandler<DefaultTokenHandler>();

// Tarayıcı kimlik doğrulamalı tüm çağrıları same-origin '/bff/*' ile yapar; kullanıcı JWT'si
// burada session'dan okunup Authorization header'ı olarak API'ye eklenir. Token tarayıcıya hiç sızmaz.
var bffRoutes = new[]
{
    new RouteConfig
    {
        RouteId = "bff",
        ClusterId = "api",
        Match = new RouteMatch { Path = "/bff/{**catch-all}" }
    }
    .WithTransformPathRemovePrefix("/bff")
};

var bffClusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "api",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["api"] = new DestinationConfig { Address = apiBaseUrl }
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(bffRoutes, bffClusters)
    .AddTransforms(transforms =>
    {
        transforms.AddRequestTransform(async ctx =>
        {
            // Session, pipeline'da UseSession ile yüklenmiş olur; garantilemek için LoadAsync.
            await ctx.HttpContext.Session.LoadAsync();
            var session = ctx.HttpContext.Session;
            var token = session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return;

            // Token'ın süresi dolmak üzereyse (≤10 dk) proaktif yenile: oturum 8 saat sürerken
            // 60 dakikalık JWT yüzünden kullanıcı saatte bir login'e düşmesin.
            var expiresRaw = session.GetString("expiresAt");
            if (DateTimeOffset.TryParse(expiresRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAt)
                && expiresAt - DateTimeOffset.UtcNow <= TimeSpan.FromMinutes(10))
            {
                var refreshed = await TryRefreshTokenAsync(ctx.HttpContext, token);
                if (refreshed is not null)
                    token = refreshed;
            }

            ctx.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
    });

var dataProtection = builder.Services.AddPersistentDataProtection(
    builder.Configuration, builder.Environment, "FurkanTural.Chat");

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Proxy sonrası state-değiştiren POST'lar cookie ile yetkilenir → CSRF'e karşı sertleştirme.
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    // SameSite=Lax (BİLİNÇLİ tercih, Strict DEĞİL): Lax zaten cross-site state-değiştiren POST'larda
    // cookie göndermez (CSRF kapalı) + formlarda AntiForgeryToken var. Strict'e çıkmak, push bildirimine
    // tıklayınca service worker'ın açık pencere yoksa clients.openWindow('/Chat') ile yaptığı üst-düzey
    // gezinmede cookie'yi keserdi → kullanıcı zorla login'e düşerdi (sw.js notificationclick). Net güvenlik
    // kazancı olmadan push UX'ini bozardı; bu yüzden Lax korunur.
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

app.LogDataProtectionStatus(dataProtection);

// EN BAŞTA olmalı: ClientLogController gerçek ziyaretçi IP'sini buradan okur.
app.UseRealClientIp(builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Chat'e özgü kısıtlamalar:
//   • SignalR: cdn.jsdelivr.net (script) + wss: (connect)
//   • Turnstile: challenges.cloudflare.com (script + frame)
//   • WebRTC: RTCPeerConnection — CSP kısıtlaması yok, tarayıcı API'si.
//   • Medya: getUserMedia (mikrofon/kamera) — CSP kısıtlaması yok.
//   • BFF: tüm /bff/* ve /hubs/* istekleri same-origin → 'self' yeterli.
//   • Inline scriptler (tema init, TempData toast, window.CHAT, Turnstile callback) artık
//     per-request NONCE ile çalışır; 'unsafe-inline' KALDIRILDI. Sohbet kullanıcı-üretimli
//     içerik (mesajlar) gösterdiğinden, olası bir HTML-enjeksiyonunda script çalışmasını
//     nonce engeller (defense-in-depth). Dış scriptler host izniyle yüklenir (nonce gerektirmez).
var apiBase = (builder.Configuration["Api:BaseUrl"] ?? "").TrimEnd('/');
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    // Kamera/mikrofon: WebRTC arama özelliği için açık; ödeme/coğrafi konum kapalı.
    headers["Permissions-Policy"] = "camera=self, microphone=self, geolocation=(), payment=()";

    // Per-request CSP nonce — inline <script> blokları yalnız bu nonce ile çalışır.
    // View'lar Context.Items["csp-nonce"] üzerinden okuyup nonce="..." olarak basar.
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    context.Items["csp-nonce"] = nonce;

    // connect-src: same-origin (/bff/* REST + WebSocket), API base, wss: (SignalR WS transport)
    var connectSrc = string.IsNullOrWhiteSpace(apiBase)
        ? "'self' wss: ws:"
        : $"'self' {apiBase} {apiBase.Replace("https://", "wss://").Replace("http://", "ws:")} wss: ws:";

    // img-src: avatar/ekler BFF üzerinden same-origin; API'den de doğrudan statik resimler gelebilir.
    var imgSrc = string.IsNullOrWhiteSpace(apiBase)
        ? "'self' data: blob:"
        : $"'self' data: blob: {apiBase}";

    headers["Content-Security-Policy"] =
        "default-src 'none'; " +
        // Inline scriptler yalnız 'nonce-...' ile; dış scriptler host izniyle (Turnstile, SignalR CDN).
        // 'strict-dynamic' KULLANILMAZ → host allowlist'i ('self' + CDN'ler) geçerli kalır, dış
        // <script src> etiketleri nonce gerektirmez. 'unsafe-inline' nonce varlığında yok sayılır.
        // Cloudflare Web Analytics beacon dış script olduğundan nonce gerektirmez; host izni yeterli.
        $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net https://challenges.cloudflare.com https://static.cloudflareinsights.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        // font-src ZORUNLU: default-src 'none' olduğu için bu direktif yokken
        // kendi sunucumuzdaki Inter dosyaları da engellenirdi.
        "font-src 'self'; " +
        "img-src " + imgSrc + "; " +
        // Turnstile widget bir iframe içinde çalışır → frame-src gerekli.
        "frame-src https://challenges.cloudflare.com; " +
        "connect-src " + connectSrc + " https://cloudflareinsights.com; " +
        // getUserMedia/WebRTC için media-src: tarayıcı API kısıtı değil ama kamera/mikrofon
        // blob URL'leri oluşturulabilir.
        "media-src 'self' blob:; " +
        "worker-src 'self'; " +
        "manifest-src 'self'; " +
        "frame-ancestors 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    await next();
});

app.UseRouting();

app.UseSession();
app.UseAuthorization();

// BFF: '/bff/*' isteklerini (REST + SignalR WebSocket) JWT enjekte ederek API'ye proxy'le.
app.MapReverseProxy();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Account", action = "Login" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();

// Mevcut (hâlâ geçerli) kullanıcı token'ı ile API'den yeni token alır; başarılıysa session'ı günceller.
// Başarısızlıkta null döner ve eldeki token'la devam edilir (401 olursa istemci login'e yönlenir).
static async Task<string?> TryRefreshTokenAsync(HttpContext httpContext, string currentToken)
{
    try
    {
        var factory = httpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("AppTokenClient"); // BaseAddress = Api:BaseUrl, ek handler yok

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Auth/refresh");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
        using var response = await client.SendAsync(request, httpContext.RequestAborted);
        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<TokenRefreshResponse>(cancellationToken: httpContext.RequestAborted);
        var data = payload?.Data;
        if (data?.Token is null)
            return null;

        httpContext.Session.SetString("token", data.Token);
        httpContext.Session.SetString("expiresAt", data.ExpiresAt.ToString("O"));
        return data.Token;
    }
    catch
    {
        return null;
    }
}

internal sealed class TokenRefreshResponse
{
    public TokenRefreshData? Data { get; set; }
}

internal sealed class TokenRefreshData
{
    public string? Token { get; set; }
    public DateTime ExpiresAt { get; set; }
}