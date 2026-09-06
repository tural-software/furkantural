using System.Net.Http.Json;

namespace FurkanTural_Admin.Services;

/// <summary>Başarısız API yanıtlarını gerçek durum kodu ve gövdesiyle birlikte günlük ucuna iletir. Panelin veri tabanına erişimi olmadığı için kayıt da API üzerinden gider ve isteğin kendi jetonu yeniden kullanılır; ayrı bir kimlik doğrulaması yoktur.<para>Kaydın bileşen adı, hatanın çıktığı API yolundan değil kullanıcının o an bulunduğu panel sayfasından türetilir: <c>Blog-Create-Post</c>. Aranan şey "hangi API ucu 500 verdi" değil "kullanıcı ne yaparken karşılaştı" olduğu için bu daha kullanışlıdır; API yolu zaten mesajda duruyor. Uygulama adını API damgalar.</para><para>Kayıt isteği beklenmez, arka planda gönderilir. Beklenseydi her hatalı çağrı kullanıcıya iki istek süresi kadar gecikmiş görünürdü. Karşılığında kaydın yazıldığı garanti değildir.</para><para>Günlük ucunun kendi yanıtları atlanır. Atlanmasaydı başarısız bir kayıt denemesi yeni bir kayıt denemesi doğurur ve bu kendini besleyerek sürerdi.</para></summary>
public class ApiFailureLoggingHandler(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ApiFailureLoggingHandler> logger)
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

    /// <summary>Kullanıcının bulunduğu panel sayfası: controller, action ve isteğin fiili. Bağlam yoksa (uygulama açılışındaki bir çağrı, arka plan işi) boş döner ve kayıt yalnızca uygulama adıyla damgalanır.</summary>
    private string? CurrentPage()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return null;

        var route = context.GetRouteData().Values;
        var verb = context.Request.Method;
        return string.Join('-', new[]
        {
            route["controller"] as string,
            route["action"] as string,
            verb.Length == 0 ? null : char.ToUpperInvariant(verb[0]) + verb[1..].ToLowerInvariant()
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    /// <summary>Gereken her değer çağrıdan önce yerele kopyalanır: istek nesnesi bu noktadan sonra serbest bırakılabilir ve arka plandaki gönderim ona erişmeye çalışırsa düşerdi.</summary>
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
            component = CurrentPage()
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
