using System.Net.Http.Json;

namespace FurkanTural_Admin.Services;

/// <summary>
/// Tüm API çağrılarını saran handler: başarısız (non-2xx) yanıtları gerçek durum kodu + gövde ile
/// API'nin AdminOnly log uç noktasına (<c>/api/v1/log</c>) iletir; böylece hatalar SQL log
/// tablosunda görünür hale gelir. Admin'in doğrudan DB erişimi olmadığından loglama API üzerinden
/// yapılır ve istek üzerindeki Bearer token tekrar kullanılır.
///
/// Log POST'u arka planda (fire-and-forget) gönderilir → kullanıcı isteğini yavaşlatmaz. Log
/// uç noktasının kendi yanıtları atlanır (recursion koruması) ve iletim başarısız olsa bile
/// asıl istek etkilenmez.
/// </summary>
public class ApiFailureLoggingHandler(IHttpClientFactory httpClientFactory, ILogger<ApiFailureLoggingHandler> logger)
    : DelegatingHandler
{
    private const string LogPath = "/api/v1/log";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // Başarılı yanıtları ve log uç noktasının kendisini (recursion) atla.
        if (response.IsSuccessStatusCode || IsLogEndpoint(request.RequestUri))
            return response;

        try
        {
            // Gövdeyi buffer'la → hem burada loglamak için hem de asıl çağıran okuyabilsin.
            await response.Content.LoadIntoBufferAsync();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Forward(request, (int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "API hata logu hazırlanamadı.");
        }

        return response;
    }

    private static bool IsLogEndpoint(Uri? uri)
        => uri is not null && uri.AbsolutePath.EndsWith(LogPath, StringComparison.OrdinalIgnoreCase);

    // Arka planda logu API'ye iletir. Gereken her şey önce locale alınır; request bu çağrıdan
    // sonra dispose edilebilir.
    private void Forward(HttpRequestMessage request, int statusCode, string body)
    {
        if (request.RequestUri is null) return;

        var method = request.Method.Method;
        var path = request.RequestUri.AbsolutePath;
        var logUri = new Uri(request.RequestUri, LogPath);
        var auth = request.Headers.Authorization; // mevcut Bearer token'ı yeniden kullan

        var payload = new
        {
            level = statusCode >= 500 ? "Error" : "Warning",
            message = $"{method} {path} -> {statusCode}",
            detail = body,
            path,
            project = "FurkanTural_Admin"
        };

        _ = Task.Run(async () =>
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, logUri);
                if (auth is not null) req.Headers.Authorization = auth;
                req.Content = JsonContent.Create(payload);

                using var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                await client.SendAsync(req);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "API hata logu SQL'e iletilemedi.");
            }
        });
    }
}
