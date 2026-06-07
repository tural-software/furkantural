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
        // Log to database via ILogService (scoped — must resolve from request scope)
        try
        {
            var logService = context.RequestServices.GetService<ILogService>();
            if (logService is not null)
            {
                var clock = context.RequestServices.GetService<IClock>();
                await logService.CreateAsync(new CreateLogDto
                {
                    Project = "FurkanTural_API",
                    Date = clock?.UtcNow ?? DateTime.UtcNow,
                    Level = "Error",
                    Message = ex.Message,
                    Detail = ex.ToString(),
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    Path = context.Request.Path
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
}