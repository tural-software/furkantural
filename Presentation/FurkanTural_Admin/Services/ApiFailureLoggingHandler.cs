using System.Net.Http.Json;

namespace FurkanTural_Admin.Services;

/// <summary>
/// Başarısız API yanıtlarını gerçek durum kodu ve gövdesiyle birlikte günlük ucuna iletir. Panelin
/// veri tabanına erişimi olmadığı için kayıt da API üzerinden gider ve isteğin kendi jetonu yeniden
/// kullanılır; ayrı bir kimlik doğrulaması yoktur.
///
/// Kayıt isteği beklenmez, arka planda gönderilir. Beklenseydi her hatalı çağrı kullanıcıya iki
/// istek süresi kadar gecikmiş görünürdü. Karşılığında kaydın yazıldığı garanti değildir.
///
/// Günlük ucunun kendi yanıtları atlanır. Atlanmasaydı başarısız bir kayıt denemesi yeni bir kayıt
/// denemesi doğurur ve bu kendini besleyerek sürerdi.
/// </summary>
public class ApiFailureLoggingHandler(IHttpClientFactory httpClientFactory, ILogger<ApiFailureLoggingHandler> logger)
    : DelegatingHandler
{
    private const string LogPath = "/api/v1/log";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode || IsLogEndpoint(request.RequestUri))
            return response;

        try
        {
            // Gövde tamponlanır; okunmadan bırakılırsa asıl çağıran boş gövde görür.
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

    /// <summary>
    /// Gereken her değer çağrıdan önce yerele kopyalanır: istek nesnesi bu noktadan sonra serbest
    /// bırakılabilir ve arka plandaki gönderim ona erişmeye çalışırsa düşerdi.
    /// </summary>
    private void Forward(HttpRequestMessage request, int statusCode, string body)
    {
        if (request.RequestUri is null) return;

        var method = request.Method.Method;
        var path = request.RequestUri.AbsolutePath;
        var logUri = new Uri(request.RequestUri, LogPath);
        var auth = request.Headers.Authorization;

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