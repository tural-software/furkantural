using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Chat.Controllers;

/// <summary>
/// Tarayıcıdan gelen istemci-tarafı logları (hata/uyarı/bilgi) app-token ile API'ye iletir.
/// App-token tarayıcıya sızmaz; relay sunucu tarafında "ApiClient" (DefaultTokenHandler) ekler.
/// Anonim erişilebilir → login/kayıt sayfalarındaki hatalar da loglanabilir.
/// </summary>
public class ClientLogController(IHttpClientFactory httpClientFactory) : Controller
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    [HttpPost("/client-log")]
    public async Task<IActionResult> Post([FromBody] ClientLogInput input, CancellationToken cancellationToken)
    {
        // Loglama hiçbir koşulda istemciyi hataya düşürmemeli; sessizce yut, her durumda 204 dön.
        try
        {
            if (input is not null && !string.IsNullOrWhiteSpace(input.Message))
            {
                var userId = HttpContext.Session.GetInt32("userId");
                var userAgent = Request.Headers.UserAgent.ToString();
                // Tanı için bağlam: kullanıcı (varsa) + tarayıcı bilgisi istemci detayına eklenir.
                var detail = $"userId={(userId?.ToString() ?? "-")} | ua={userAgent} | {input.Detail}";

                var client = _httpClientFactory.CreateClient("ApiClient");
                await client.PostAsJsonAsync("/api/v1/clientlog", new
                {
                    level = input.Level,
                    message = input.Message,
                    detail,
                    path = input.Path,
                    ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                }, cancellationToken);
            }
        }
        catch { /* yutuldu */ }

        return NoContent();
    }

    public class ClientLogInput
    {
        public string? Level { get; set; }
        public string? Message { get; set; }
        public string? Detail { get; set; }
        public string? Path { get; set; }
    }
}
