using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Sağlayıcı Cloudflare Realtime'dır; <c>Cloudflare:Realtime:TurnKeyId</c> ve <c>Cloudflare:Realtime:TurnApiToken</c> ile çağrılır. Cloudflare <c>iceServers</c> alanını kimi zaman dizi kimi zaman tek nesne olarak döndürür, ikisi de aynı listeye normalize edilir.<para>İstenen ömür 24 saattir. Dış çağrının her hatası — ağ, yetki, çözümleme — kullanıcıya tek bir "ulaşılamadı" yanıtına iner; ayrımı yalnızca durum kodu taşır.</para></summary>
public class TurnCredentialProvider(IConfiguration configuration, IHttpClientFactory httpClientFactory) : ITurnCredentialProvider
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private const int TtlSeconds = 86400;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<Result<TurnCredentialsDto>> GetIceServersAsync(int? customIdentifier = null, CancellationToken cancellationToken = default)
    {
        var keyId = _configuration["Cloudflare:Realtime:TurnKeyId"];
        var apiToken = _configuration["Cloudflare:Realtime:TurnApiToken"];

        if (IsUnset(keyId) || IsUnset(apiToken))
            return Result<TurnCredentialsDto>.Fail("Arama altyapısı (TURN) yapılandırılmamış.", statusCode: 503);

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://rtc.live.cloudflare.com/v1/turn/keys/{keyId}/credentials/generate-ice-servers")
            {
                Content = JsonContent.Create(new
                {
                    ttl = TtlSeconds,
                    customIdentifier = customIdentifier?.ToString()
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<TurnCredentialsDto>.Fail("Arama kimlik bilgileri alınamadı.", statusCode: 502);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("iceServers", out var ice))
                return Result<TurnCredentialsDto>.Fail("Arama kimlik bilgileri çözümlenemedi.", statusCode: 502);

            var servers = (ice.ValueKind switch
            {
                JsonValueKind.Array => ice.Deserialize<IceServerDto[]>(JsonOpts) ?? [],
                JsonValueKind.Object => [ice.Deserialize<IceServerDto>(JsonOpts)!],
                _ => Array.Empty<IceServerDto>()
            }).Where(s => s?.Urls is { Length: > 0 }).ToArray();

            if (servers.Length == 0)
                return Result<TurnCredentialsDto>.Fail("Arama kimlik bilgileri çözümlenemedi.", statusCode: 502);

            return Result<TurnCredentialsDto>.Ok(new TurnCredentialsDto { IceServers = servers });
        }
        catch
        {
            return Result<TurnCredentialsDto>.Fail("Arama altyapısına ulaşılamadı.", statusCode: 502);
        }
    }

    private static bool IsUnset(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
}
