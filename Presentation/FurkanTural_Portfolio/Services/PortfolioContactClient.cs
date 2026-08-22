using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FurkanTural_Portfolio.Models;

namespace FurkanTural_Portfolio.Services;

public class PortfolioContactClient(HttpClient httpClient, ILogger<PortfolioContactClient> logger) : IPortfolioContactClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<PortfolioContactClient> _logger = logger;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<bool> SubmitContactAsync(ContactFormModel model, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                name = model.Name,
                email = model.Email,
                message = model.Message,
                turnstileToken = model.TurnstileToken
            };

            var json = JsonSerializer.Serialize(payload, WriteOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync("/api/v1/contact/submit", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "İletişim formu gönderilemedi.");
            return false;
        }
    }
}