using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Blog.Models.Wrappers;

namespace FurkanTural_Blog.Services;

/// <summary>Uygulama jetonuyla <c>POST /api/v1/subscriber/subscribe</c> çağırır. Uç <c>VisitorOrAbove</c> ister ve uygulama jetonu Visitor rolü taşıdığı için ek bir kimlik gerekmez; ziyaretçi oturum açmaz.<para>Başarısız yanıtta okunacak metin <c>Errors[0]</c>'dadır, <c>Message</c>'ta değil — zarf Ok yolunda Message'ı, Fail yolunda Errors'ı doldurur.</para></summary>
public class NewsletterClient(HttpClient httpClient, ILogger<NewsletterClient> logger) : INewsletterClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<NewsletterClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<NewsletterOutcome> SubscribeAsync(string email, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/subscriber/subscribe", new { email }, ct);
            var body = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions, ct);

            if (body is null)
                return new NewsletterOutcome(response.IsSuccessStatusCode, null);

            return new NewsletterOutcome(body.Success, body.Success ? body.Message : body.Errors.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bülten aboneliği kaydedilemedi.");
            return new NewsletterOutcome(false, null);
        }
    }
}
