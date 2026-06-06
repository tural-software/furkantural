using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FurkanTural_Business.Registration;
using FurkanTural_Persistence.Registration;
using FurkanTural_Persistence.Contexts;
using FurkanTural_API.Middlewares;
using FurkanTural_API.Hubs;
using FurkanTural_API.Realtime;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// --- Şifreli config değerlerini startup'ta çöz ---
{
    var encKey = builder.Configuration["EncryptionSettings:Key"]
        ?? throw new InvalidOperationException("EncryptionSettings:Key yapılandırılmamış.");
    var encIv = builder.Configuration["EncryptionSettings:IV"]
        ?? throw new InvalidOperationException("EncryptionSettings:IV yapılandırılmamış.");

    var pattern = new System.Text.RegularExpressions.Regex(@"^(\d{4}):(.+):(\d{4})$");
    var overrides = new Dictionary<string, string?>();

    foreach (var kv in builder.Configuration.AsEnumerable())
    {
        if (kv.Value is null) continue;
        if (kv.Key.StartsWith("EncryptionSettings:")) continue;

        var m = pattern.Match(kv.Value);
        if (!m.Success) continue;

        try
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(encKey);
            aes.IV  = Encoding.UTF8.GetBytes(encIv);

            using var ms = new MemoryStream(Convert.FromBase64String(m.Groups[2].Value));
            using var cs = new System.Security.Cryptography.CryptoStream(ms, aes.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            var decrypted = sr.ReadToEnd();

            var s1 = m.Groups[1].Value;
            var s2 = m.Groups[3].Value;
            if (decrypted.StartsWith(s1, StringComparison.Ordinal) && decrypted.EndsWith(s2, StringComparison.Ordinal))
                overrides[kv.Key] = decrypted[s1.Length..^s2.Length];
        }
        catch (System.Security.Cryptography.CryptographicException) { /* şifreli değil, atla */ }
    }

    if (overrides.Count > 0)
        builder.Configuration.AddInMemoryCollection(overrides);
}

// --- Services ---

builder.Services.AddHttpClient();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddBusinessServices();
builder.Services.AddHttpContextAccessor();

// SignalR (gerçek zamanlı sohbet + bildirimler)
builder.Services.AddSignalR();
builder.Services.AddScoped<IChatNotifier, ChatNotifier>();
builder.Services.AddSingleton<IUserIdProvider, SubUserIdProvider>();
builder.Services.Configure<AppTokenSettings>(builder.Configuration.GetSection("AppTokens"));
builder.Services.AddSingleton(new FurkanTural_Application.Settings.FileStorageSettings
{
    // Yüklemeler wwwroot kökü altında modül/medya-türü klasörlerine yazılır (FileService oluşturur).
    WebRootPath = builder.Environment.WebRootPath
});

builder.Services.AddControllers();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret yapılandırılmamış.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        // SignalR tarayıcı istemcisi token'ı query string ile gönderir (WebSocket).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",      policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin",    policy => policy.RequireRole("Admin", "User"));
    options.AddPolicy("VisitorOrAbove", policy => policy.RequireRole("Admin", "User", "Subscriber", "Visitor"));
    // Yalnızca app-token sahibi (kayıtlı ön-yüz) erişebilir — app-token her zaman app_source claim'i taşır.
    options.AddPolicy("AppClient",      policy => policy.RequireClaim("app_source"));
});

// Swagger — yalnızca etkinleştirildiğinde kayıt yapılır
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled");
builder.Services.AddEndpointsApiExplorer();
if (swaggerEnabled)
{
    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen(options =>
    {
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPath);
    
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT token giriniz. Swagger otomatik 'Bearer ' ekler, sadece token'ı yapıştırın."
        });
    
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });
}

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Static files (for wwwroot/images/uploads)
builder.Services.AddDirectoryBrowser();

var app = builder.Build();

// --- Bekleyen EF migration'larını başlangıçta otomatik uygula ---
if (builder.Configuration.GetValue<bool?>("Database:ApplyMigrationsOnStartup") ?? true)
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<FurkanTuralDbContext>();
    try
    {
        // DB yoksa oluşturur, bekleyen migration'ları uygular, günceldeyse no-op. (EF Core eşzamanlı başlatmada kilit alır.)
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Veritabanı migration kontrolü tamamlandı (bekleyenler uygulandı).");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Başlangıçta migration uygulanamadı; uygulama durduruluyor.");
        throw; // fail-fast: bozuk migration sessizce geçmesin
    }
}

// --- Middleware pipeline ---

app.UseMiddleware<ExceptionMiddleware>();

if (swaggerEnabled)
{
    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments("/swagger"),
        branch => branch.Use(async (ctx, next) =>
        {
            var requestOrigin = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";
            if (!allowedOrigins.Any(o => string.Equals(o, requestOrigin, StringComparison.OrdinalIgnoreCase)))
            {
                ctx.Response.StatusCode = 404;
                return;
            }
            await next();
        })
    );

    var apiVersionDescriptions = app.Services
        .GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        foreach (var description in apiVersionDescriptions.ApiVersionDescriptions)
        {
            ui.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"FurkanTural API {description.GroupName.ToUpperInvariant()}");
        }
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

// --- Swagger versiyonlama yapılandırması ---

internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "FurkanTural API",
                Version = description.GroupName,
                Description = description.IsDeprecated
                    ? "Bu API versiyonu kullanımdan kaldırılmıştır."
                    : string.Empty
            });
        }
    }
}
