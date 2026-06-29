using System.Net;
using System.Text.Json;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;

namespace FurkanTural_API.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlenmemiş exception: {Message}", ex.Message);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Logu TAZE bir DI scope'tan yaz: exception bir DbUpdateException ise request'in
        // scoped DbContext'i "faulted" olur ve aynı context üzerinden log yazımı da patlar
        // (ve sessizce yutulurdu → 500 olur ama hiç log kalmazdı). Yeni scope = temiz DbContext.
        try
        {
            using var scope = context.RequestServices
                .GetRequiredService<IServiceScopeFactory>().CreateScope();
            var logService = scope.ServiceProvider.GetService<ILogService>();
            if (logService is not null)
            {
                var clock = scope.ServiceProvider.GetService<IClock>();
                await logService.CreateAsync(new CreateLogDto
                {
                    Project = "FurkanTural_API",
                    Date = clock?.UtcNow ?? DateTime.UtcNow,
                    Level = "Error",
                    // Message/Path, DB kolonlarını (nvarchar(1000)/(500)) aşabilir; ikincil
                    // DbUpdateException'ı (ve sessizce yutulan kayıp logu) önlemek için kırp.
                    // Detail nvarchar(max) olduğundan güvenli.
                    Message = Truncate(ex.Message, 1000),
                    Detail = ex.ToString(),
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    Path = Truncate(context.Request.Path.Value, 500)
                });
            }
        }
        catch
        {
            // DB log başarısız olsa bile yanıt döndürmeye devam et
        }

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            statusCode = (int)HttpStatusCode.InternalServerError,
            errors = new[] { "Sunucu tarafında beklenmeyen bir hata oluştu." }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}