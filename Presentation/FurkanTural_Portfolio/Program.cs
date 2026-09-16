using FurkanTural_Portfolio;
using FurkanTural_Portfolio.Middlewares;
using FurkanTural_Portfolio.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews(options => options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()))
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ClientForwardingHandler>();

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7000");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(FastFailHandler)
  .AddHttpMessageHandler<DefaultTokenHandler>()
  .AddHttpMessageHandler<ClientForwardingHandler>();

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

var dataProtection = builder.Services.AddPersistentDataProtection(
    builder.Configuration, builder.Environment, "FurkanTural.Portfolio");

var app = builder.Build();

app.LogDataProtectionStatus(dataProtection);

app.UseRealClientIp(builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
    context.Items["csp-nonce"] = nonce;
    var headers = context.Response.Headers;

    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "SAMEORIGIN";

    // Tarayıcı özellik politikası: kamera/mikrofon/konum/ödeme gereksinimi yok (Blog/Chat ile tutarlı).
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

    // 'unsafe-eval' eklenmedi: hiçbir yerde eval/Function() kullanımı yok.
    // frame-src: Turnstile doğrulama widget'ı challenges.cloudflare.com iframe'i açar.
    // img-src https: — API sunucusu (proje/müzik görselleri) domain'i config'e göre değişir;
    //   'self' + https: ile tüm HTTPS origin'lere izin verildi.
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        $"script-src 'self' 'nonce-{nonce}' https://challenges.cloudflare.com; " +
        $"style-src 'self' 'nonce-{nonce}'; " +
        // Inter kendi sunucumuzda barındırılıyor → üçüncü-taraf font alanına gerek yok.
        "font-src 'self'; " +
        "img-src 'self' https: data:; " +
        "connect-src 'self'; " +
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