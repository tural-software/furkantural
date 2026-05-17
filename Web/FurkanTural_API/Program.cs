using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FurkanTural_Business.Registration;
using FurkanTural_Persistence.Registration;
using FurkanTural_API.Middlewares;
using FurkanTural_Application.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddBusinessServices();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<AppTokenSettings>(builder.Configuration.GetSection("AppTokens"));
builder.Services.AddSingleton(new FurkanTural_Application.Settings.FileStorageSettings
{
    UploadsPath = Path.Combine(builder.Environment.WebRootPath, "images", "uploads")
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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",      policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin",    policy => policy.RequireRole("Admin", "User"));
    options.AddPolicy("VisitorOrAbove", policy => policy.RequireRole("Admin", "User", "Subscriber", "Visitor"));
});

// Swagger — her versiyon için ayrı SwaggerDoc
builder.Services.AddEndpointsApiExplorer();
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

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Static files (for wwwroot/images/uploads)
builder.Services.AddDirectoryBrowser();

var app = builder.Build();

// --- Middleware pipeline ---

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
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
