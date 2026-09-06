using System.Text.Json;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Business.Helpers;
using FurkanTural_Domain.Constants;
using FurkanTural_Application.Exceptions;
using FurkanTural_Application.Services.Abstract;

namespace FurkanTural_API.Middlewares;

/// <summary>Yakalanmamış istisnaları sabit metinli bir yanıta çevirir. İstemciye tür, mesaj veya yığın bilgisi geçmez; bu yüzden birbirinden çok farklı sebepler dışarıdan aynı yanıtı üretir ve ayrım yalnızca kaydedilen günlükte kalır.<para>Tek ayrıcalık <see cref="PersistenceConflictException"/> soyundan gelenlerdir: onlar 500 değil 409 üretir ve kayda Error değil Warning düşer. Sebebi, bunların sunucu arızası olmamasıdır — iki isteğin aynı satırı yazmaya çalışması beklenen bir çekişmedir, 500 dönmek hem istemciyi yanıltır hem de gerçek arızaları günlükte görünmez eder. Kısıt adı istisnanın içinde taşınır ve yalnızca kayda yazılır; istemciye şema bilgisi çıkmaz.</para><para>İstemcinin kendisinin vazgeçtiği istek (sekme kapandı, gezinme değişti) arıza değildir: iptal istisnası, istek iptal edilmişken yakalanırsa günlüğe Error düşmez, Logs tablosuna satır yazılmaz ve yanıt 499 ile sessizce kapanır. Aynı istisna istek hâlâ canlıyken gelirse iç bir zaman aşımıdır ve 500 olarak kalır.</para></summary>
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("İstemci isteği yarıda kesti: {Path}", context.Request.Path);
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            var outcome = Classify(ex);

            if (outcome.IsConflict)
                _logger.LogWarning(ex, "Veri tabanı kısıtı isteği reddetti: {Message}", ex.Message);
            else
                _logger.LogError(ex, "İşlenmemiş exception: {Message}", ex.Message);

            await HandleExceptionAsync(context, ex, outcome);
        }
    }

    private static Outcome Classify(Exception ex) => ex switch
    {
        DuplicateEntityException => new Outcome(409, "Bu kayıt zaten var.", "Warning", true),
        RelatedEntityMissingException => new Outcome(409, "İlişkili kayıt bulunamadı.", "Warning", true),
        _ => new Outcome(500, "Sunucu tarafında beklenmeyen bir hata oluştu.", "Error", false)
    };

    private readonly record struct Outcome(int StatusCode, string ClientMessage, string Level, bool IsConflict);

    /// <summary>Günlük kaydı isteğin kendi kapsamından değil, yeni açılan bir kapsamdan yazılır. Sebebi: istisna bir kayıt hatasıysa isteğin veri bağlamı bozulmuş durumdadır ve aynı bağlam üzerinden yazma denemesi de düşerdi. O hâlde istek hata dönerdi ama geriye hiçbir kayıt kalmazdı.<para>Mesaj ve yol, hedef kolonların genişliğine kırpılır; aksi hâlde günlüğü yazma denemesi ikinci bir kayıt hatası doğururdu. Detay alanı sınırsız olduğundan kırpılmaz.</para><para>Kaydın kendisi başarısız olursa yutulur ve yanıt yine döner: hata yanıtı, günlüğün yazılıp yazılamadığına bağlı kalmamalıdır.</para></summary>
    private static async Task HandleExceptionAsync(HttpContext context, Exception ex, Outcome outcome)
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
                    Source = LogSourceBuilder.FromContext(context, LogSources.Api),
                    Date = clock?.UtcNow ?? DateTime.UtcNow,
                    Level = outcome.Level,
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

        context.Response.StatusCode = outcome.StatusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            statusCode = outcome.StatusCode,
            errors = new[] { outcome.ClientMessage }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
