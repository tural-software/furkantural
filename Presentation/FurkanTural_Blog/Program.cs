using FurkanTural_Blog;
using FurkanTural_Blog.Middlewares;
using FurkanTural_Blog.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
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

builder.Services.AddScoped<IBlogApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("ApiClient");
    var logger = sp.GetRequiredService<ILogger<BlogApiService>>();
    return new BlogApiService(client, logger);
});

builder.Services.AddScoped<INewsletterClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("ApiClient");
    var logger = sp.GetRequiredService<ILogger<NewsletterClient>>();
    return new NewsletterClient(client, logger);
});

builder.Services.AddScoped<ICommentClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("ApiClient");
    var logger = sp.GetRequiredService<ILogger<CommentClient>>();
    return new CommentClient(client, logger);
});

var dataProtection = builder.Services.AddPersistentDataProtection(
    builder.Configuration, builder.Environment, "FurkanTural.Blog");

var app = builder.Build();

app.LogDataProtectionStatus(dataProtection);

app.UseRealClientIp(builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// img-src: yerel varlıklar ('self') + API'den gelen kapak görselleri (Api:BaseUrl) + data URI
// (Service Worker önbelleği için 'self' yeterli; API origin eklenmesi zorunlu çünkü
//  kapak URL'leri BuildImageUrl() ile API sunucusundan mutlak adres olarak oluşturulur).
// worker-src: Service Worker kaydı için ('self') gerekli.
var apiBase = (builder.Configuration["Api:BaseUrl"] ?? "").TrimEnd('/');
app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
    context.Items["csp-nonce"] = nonce;
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

    var imgSrc = string.IsNullOrWhiteSpace(apiBase)
        ? "'self' data:"
        : $"'self' data: {apiBase}";
    headers["Content-Security-Policy"] =
        "default-src 'none'; " +
        $"script-src 'self' 'nonce-{nonce}' https://challenges.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        // Inter kendi sunucumuzda barındırılıyor → üçüncü-taraf font alanına gerek yok.
        "font-src 'self'; " +
        $"img-src {imgSrc}; " +
        "connect-src 'self'; " +
        "frame-src https://challenges.cloudflare.com; " +
        "manifest-src 'self'; " +
        "worker-src 'self'; " +
        "frame-ancestors 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    await next();
});

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