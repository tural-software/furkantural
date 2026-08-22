using System.Net;
using System.Text.Json;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;

namespace FurkanTural_API.Middlewares;

/// <summary>
/// Yakalanmamış her istisnayı sabit metinli bir 500'e çevirir. İstemciye tür, mesaj veya yığın
/// bilgisi geçmez; bu yüzden birbirinden çok farklı sebepler dışarıdan aynı yanıtı üretir ve ayrım
/// yalnızca kaydedilen günlükte kalır.
/// </summary>
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

    /// <summary>
    /// Günlük kaydı isteğin kendi kapsamından değil, yeni açılan bir kapsamdan yazılır. Sebebi:
    /// istisna bir kayıt hatasıysa isteğin veri bağlamı bozulmuş durumdadır ve aynı bağlam üzerinden
    /// yazma denemesi de düşerdi. O hâlde istek 500 dönerdi ama geriye hiçbir kayıt kalmazdı.
    ///
    /// Mesaj ve yol, hedef kolonların genişliğine kırpılır; aksi hâlde günlüğü yazma denemesi ikinci
    /// bir kayıt hatası doğururdu. Detay alanı sınırsız olduğundan kırpılmaz.
    ///
    /// Kaydın kendisi başarısız olursa yutulur ve yanıt yine döner: hata yanıtı, günlüğün yazılıp
    /// yazılamadığına bağlı kalmamalıdır.
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
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
                    Message = Truncate(ex.Message, 1000),
                    Detail = ex.ToString(),
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    Path = Truncate(context.Request.Path.Value, 500)
                });
            }
        }
        catch
        {
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