using System.Net.Http.Headers;
using FurkanTural_Chat;
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
});
builder.Services.AddSingleton<IAppTokenService, AppTokenService>();
builder.Services.AddSingleton<IAppConfigService, AppConfigService>();
builder.Services.AddTransient<DefaultTokenHandler>();
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<DefaultTokenHandler>();

// ───────── BFF reverse proxy (YARP, aynı process — Docker/ayrı servis yok) ─────────
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
            var token = ctx.HttpContext.Session.GetString("token");
            if (!string.IsNullOrEmpty(token))
                ctx.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Proxy sonrası state-değiştiren POST'lar cookie ile yetkilenir → CSRF'e karşı sertleştirme.
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
