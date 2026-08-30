using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace FurkanTural_Business.Services.Concrete;

public class TurnstileVerifier(IConfiguration configuration, IHttpClientFactory httpClientFactory) : ITurnstileVerifier
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var secret = _configuration["Turnstile:SecretKey"];

        if (string.IsNullOrWhiteSpace(secret) || secret.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = token,
                ["remoteip"] = remoteIp ?? ""
            });
            var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(cancellationToken: cancellationToken);
            return result?.Success ?? false;
        }
        catch
        {
            return false;
        }
    }

    private sealed class TurnstileResponse
    {
        public bool Success { get; set; }
    }
}