using FurkanTural_Blog;
using FurkanTural_Blog.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// API erişilemez olduğunda sayfaların asılı kalmaması için kısa bağlantı zaman aşımı.
static SocketsHttpHandler FastFailHandler() => new()
{
    ConnectTimeout = TimeSpan.FromSeconds(2),
    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
};

// API entegrasyonu — uygulama token servisi
builder.Services.AddHttpClient("AppTokenClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(FastFailHandler);

builder.Services.AddSingleton<IAppTokenService, AppTokenService>();
builder.Services.AddTransient<DefaultTokenHandler>();

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(FastFailHandler)
  .AddHttpMessageHandler<DefaultTokenHandler>();

// Blog services
builder.Services.AddScoped<IBlogApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("ApiClient");
    var logger = sp.GetRequiredService<ILogger<BlogApiService>>();
    return new BlogApiService(client, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Temel güvenlik başlıkları (her yanıta uygulanır).
// img-src: yerel varlıklar ('self') + API'den gelen kapak görselleri (Api:BaseUrl) + data URI
// (Service Worker önbelleği için 'self' yeterli; API origin eklenmesi zorunlu çünkü
//  kapak URL'leri BuildImageUrl() ile API sunucusundan mutlak adres olarak oluşturulur).
// script-src: 'unsafe-inline' — head'deki FOUC-önleme inline bloğu ve JSON-LD @Html.Raw()
//  çıktıları nedeniyle zorunlu. Nonce yaklaşımı bu projede aşırı karmaşıklık getirir;
//  'unsafe-inline' burada kabul edilebilir çünkü tüm inline içerik sunucu tarafından
//  üretilen sabit değerlerdir (kullanıcı girdisi inline script'e dönüşmüyor).
// worker-src: Service Worker kaydı için ('self') gerekli.
var apiBase = (builder.Configuration["Api:BaseUrl"] ?? "").TrimEnd('/');
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

    // Content-Security-Policy
    var imgSrc = string.IsNullOrWhiteSpace(apiBase)
        ? "'self' data:"
        : $"'self' data: {apiBase}";
    headers["Content-Security-Policy"] =
        "default-src 'none'; " +
        // Cloudflare Web Analytics beacon'ı (static.cloudflareinsights.com) önde enjekte edilir.
        "script-src 'self' 'unsafe-inline' https://static.cloudflareinsights.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        $"img-src {imgSrc}; " +
        "connect-src 'self' https://cloudflareinsights.com; " +
        "manifest-src 'self'; " +
        "worker-src 'self'; " +
        "frame-ancestors 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    await next();
});

// 404 vb. durum kodlarını markalı hata sayfasına yönlendir.
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
