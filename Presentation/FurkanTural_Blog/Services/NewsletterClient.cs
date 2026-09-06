using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Blog.Models.Wrappers;

namespace FurkanTural_Blog.Services;

/// <summary>Uygulama jetonuyla <c>subscriber/*</c> uçlarını çağırır. Uçlar <c>VisitorOrAbove</c> ister ve uygulama jetonu Visitor rolü taşıdığı için ek bir kimlik gerekmez; ziyaretçi oturum açmaz.<para>Başarısız yanıtta okunacak metin <c>Errors[0]</c>'dadır, <c>Message</c>'ta değil — zarf Ok yolunda Message'ı, Fail yolunda Errors'ı doldurur.</para></summary>
public class NewsletterClient(HttpClient httpClient, ILogger<NewsletterClient> logger) : INewsletterClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<NewsletterClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<NewsletterOutcome> SubscribeAsync(string email, string? turnstileToken, CancellationToken ct = default)
        => PostAsync("subscribe", new { email, turnstileToken }, "Bülten aboneliği kaydedilemedi.", ct);

    public Task<NewsletterOutcome> ConfirmAsync(string token, CancellationToken ct = default)
        => PostAsync("confirm", new { token }, "Bülten aboneliği doğrulanamadı.", ct);

    public Task<NewsletterOutcome> RequestUnsubscribeAsync(string email, CancellationToken ct = default)
        => PostAsync("request-unsubscribe", new { email }, "Çıkış bağlantısı istenemedi.", ct);

    public Task<NewsletterOutcome> UnsubscribeAsync(string token, CancellationToken ct = default)
        => PostAsync("unsubscribe", new { token }, "Abonelik iptal edilemedi.", ct);

    private async Task<NewsletterOutcome> PostAsync(string path, object body, string failureLog, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/subscriber/{path}", body, ct);
            var envelope = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions, ct);

            if (envelope is null)
                return new NewsletterOutcome(response.IsSuccessStatusCode, null);

            return new NewsletterOutcome(envelope.Success, envelope.Success ? envelope.Message : envelope.Errors.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Message}", failureLog);
            return new NewsletterOutcome(false, null);
        }
    }
}
