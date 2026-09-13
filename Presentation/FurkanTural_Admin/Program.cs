using System.Net.Http.Headers;
using FurkanTural_Admin;
using FurkanTural_Admin.Middlewares;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Services;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiFailureLoggingHandler>();
builder.Services.AddTransient<ClientForwardingHandler>();
builder.Services.ConfigureHttpClientDefaults(http => http
    .AddHttpMessageHandler<ApiFailureLoggingHandler>()
    .AddHttpMessageHandler<ClientForwardingHandler>());

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl yapılandırılmamış.");

builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("AppTokenClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

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
            ClientForwarding.Apply(ctx.HttpContext, ctx.ProxyRequest.Headers);

            await ctx.HttpContext.Session.LoadAsync();
            var session = ctx.HttpContext.Session;
            var token = session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return;

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

builder.Services.AddHttpClient<IAdminDashboardClient, AdminDashboardClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<ISchemaApiClient, SchemaApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<ISubscriberApiClient, SubscriberApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IAdminSearch, AdminSearch>();
builder.Services.AddHttpClient<ISkillApiClient, SkillApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IBlogApiClient, BlogApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ICategoryApiClient, CategoryApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ITagApiClient, TagApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ICommentApiClient, CommentApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IBlogImageApiClient, BlogImageApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IExperienceApiClient, ExperienceApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IEducationApiClient, EducationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ILogApiClient, LogApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IRoleApiClient, RoleApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IUserApiClient, UserApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IMusicApiClient, MusicApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IMusicImageApiClient, MusicImageApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IProjectApiClient, ProjectApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IProjectImageApiClient, ProjectImageApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IContactApiClient, ContactApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IMailTemplateApiClient, MailTemplateApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<INewsletterIssueApiClient, NewsletterIssueApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IStatusApiClient, StatusApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IUserFriendApiClient, UserFriendApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IChatMessageApiClient, ChatMessageApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ICallLogApiClient, CallLogApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IReportApiClient, ReportApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ICallPolicyApiClient, CallPolicyApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var dataProtection = builder.Services.AddPersistentDataProtection(
    builder.Configuration, builder.Environment, "FurkanTural.Admin");

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

app.LogDataProtectionStatus(dataProtection);

app.UseRealClientIp(builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Güvenlik response header'ları — clickjacking, MIME-sniffing ve içerik enjeksiyonu korumaları.
// API görsel/medya kaynaklarını farklı origin'den sunduğundan CSP'ye yapılandırılmış API origin'i de dahil edilir.
var apiOrigin = new Uri(apiBaseUrl).GetLeftPart(UriPartial.Authority);
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        // Inter kendi sunucumuzda barındırılıyor → üçüncü-taraf font alanına gerek yok.
        "font-src 'self' data:; " +
        $"img-src 'self' data: blob: {apiOrigin}; " +
        $"media-src 'self' blob: {apiOrigin}; " +
        $"connect-src 'self' {apiOrigin} ws://{context.Request.Host} wss://{context.Request.Host}; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';";
    await next();
});

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapReverseProxy();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Auth", action = "Login" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static async Task<string?> TryRefreshTokenAsync(HttpContext httpContext, string currentToken)
{
    try
    {
        var factory = httpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("AppTokenClient");

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