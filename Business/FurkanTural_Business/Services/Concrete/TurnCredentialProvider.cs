using System.Net.Http.Headers;
using System.Net.Http.Json;
using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// Cloudflare Realtime TURN'den kısa ömürlü ICE kimlik bilgileri üretir.
/// Yapılandırma: <c>Cloudflare:Realtime:TurnKeyId</c> + <c>Cloudflare:Realtime:TurnApiToken</c> (token şifreli).
/// </summary>
public class TurnCredentialProvider(IConfiguration configuration, IHttpClientFactory httpClientFactory) : ITurnCredentialProvider
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private const int TtlSeconds = 86400; // 24 saat (Cloudflare maks 48 saat)

    public async Task<Result<TurnCredentialsDto>> GetIceServersAsync(int? customIdentifier = null, CancellationToken cancellationToken = default)
    {
        var keyId = _configuration["Cloudflare:Realtime:TurnKeyId"];
        var apiToken = _configuration["Cloudflare:Realtime:TurnApiToken"];

        if (IsUnset(keyId) || IsUnset(apiToken))
            return Result<TurnCredentialsDto>.Fail("Arama altyapısı (TURN) yapılandırılmamış.", statusCode: 503);

        try
        {
            var client = _httpClientFactory.CreateClient();
            // Cloudflare: generate-ice-servers → iceServers bir dizi (STUN + TURN) döner.
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://rtc.live.cloudflare.com/v1/turn/keys/{keyId}/credentials/generate-ice-servers")
            {
                Content = JsonContent.Create(new
                {
                    ttl = TtlSeconds,
                    customIdentifier = customIdentifier?.ToString() // kullanıcı bazlı kullanım analizi
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<TurnCredentialsDto>.Fail("Arama kimlik bilgileri alınamadı.", statusCode: 502);

            var dto = await response.Content.ReadFromJsonAsync<TurnCredentialsDto>(cancellationToken: cancellationToken);
            if (dto?.IceServers is null || dto.IceServers.Length == 0)
                return Result<TurnCredentialsDto>.Fail("Arama kimlik bilgileri çözümlenemedi.", statusCode: 502);

            return Result<TurnCredentialsDto>.Ok(dto);
        }
        catch
        {
            return Result<TurnCredentialsDto>.Fail("Arama altyapısına ulaşılamadı.", statusCode: 502);
        }
    }

    private static bool IsUnset(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
}
