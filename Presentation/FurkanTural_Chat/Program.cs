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
