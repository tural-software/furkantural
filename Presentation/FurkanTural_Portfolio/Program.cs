using FurkanTural_Portfolio;
using FurkanTural_Portfolio.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

static SocketsHttpHandler FastFailHandler() => new()
{
    ConnectTimeout = TimeSpan.FromSeconds(2),
    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
};

builder.Services.AddHttpClient("AppTokenClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(FastFailHandler);

builder.Services.AddSingleton<IAppTokenService, AppTokenService>();
builder.Services.AddSingleton<IAppConfigService, AppConfigService>();
builder.Services.AddTransient<DefaultTokenHandler>();

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(FastFailHandler)
  .AddHttpMessageHandler<DefaultTokenHandler>();

builder.Services.AddScoped<IPortfolioApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("ApiClient");
    var logger = sp.GetRequiredService<ILogger<PortfolioApiService>>();
    return new PortfolioApiService(client, logger);
});
builder.Services.AddScoped<IPortfolioContactClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("ApiClient");
    var logger = sp.GetRequiredService<ILogger<PortfolioContactClient>>();
    return new PortfolioContactClient(client, logger);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "SAMEORIGIN";

    // Tarayıcı özellik politikası: kamera/mikrofon/konum/ödeme gereksinimi yok (Blog/Chat ile tutarlı).
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

    // script-src notu: 'unsafe-inline' gerekli — sayfada sunucu-kontrollü iki inline blok var:
    //   (1) document.documentElement.classList.add('js')  (_Layout.cshtml, FOUC önleme)
    //   (2) <script type="application/ld+json"> JSON-LD blokları (_Layout + Detail sayfaları)
    // Nonce tabanlı yaklaşım, bu iki ayrı inline bloğun her birine middleware nonce enjeksiyonu
    // ve Razor tag güncellemesi gerektirir — siteyi bozma riski taşır.
    // 'unsafe-eval' eklenmedi: hiçbir yerde eval/Function() kullanımı yok.
    // script-src cdn.jsdelivr.net: dekoratif 3D sahneler (background-three.js arka plan
    //   parçacık/uydu "siber ay" sahnesi + portfolio-3d.js hero "siber Dünya"/Ay) three.js'i
    //   sürüm-sabitli (three@0.160.0) DİNAMİK import() ile bu CDN'den yükler. İzin verilmezse
    //   import bloke olur ve TÜM 3D sessizce statik içeriğe düşer. (Chat sitesi CSP'si ile
    //   tutarlı — orada da SignalR için cdn.jsdelivr.net izinlidir.)
    // frame-src: Turnstile doğrulama widget'ı challenges.cloudflare.com iframe'i açar.
    // img-src https: — API sunucusu (proje/müzik görselleri) domain'i config'e göre değişir;
    //   'self' + https: ile tüm HTTPS origin'lere izin verildi.
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        // static.cloudflareinsights.com → Cloudflare Web Analytics beacon (önde enjekte edilir).
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://challenges.cloudflare.com https://static.cloudflareinsights.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        // Inter kendi sunucumuzda barındırılıyor → üçüncü-taraf font alanına gerek yok.
        "font-src 'self'; " +
        "img-src 'self' https: data:; " +
        "connect-src 'self' https://cloudflareinsights.com; " +
        "frame-src https://challenges.cloudflare.com; " +
        "manifest-src 'self'; " +
        "worker-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'self'; " +
        "form-action 'self';";

    await next();
});

app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseHttpsRedirection();
app.UseRouting();

var supportedCultures = new[] { new CultureInfo("tr-TR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();