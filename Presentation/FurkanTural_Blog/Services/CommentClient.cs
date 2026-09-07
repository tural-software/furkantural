using System.Net.Http.Json;
using System.Text.Json;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Models.Wrappers;

namespace FurkanTural_Blog.Services;

public class CommentClient(HttpClient httpClient, ILogger<CommentClient> logger) : ICommentClient
{
    private const string Base = "/api/v1/comment";

    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CommentClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<CommentThreadViewModel> GetThreadAsync(int blogId, CancellationToken ct = default)
    {
        try
        {
            var envelope = await _httpClient.GetFromJsonAsync<ApiResult<CommentThreadViewModel>>(
                $"{Base}/blog/{blogId}", JsonOptions, ct);

            return envelope?.Data ?? new CommentThreadViewModel { BlogId = blogId };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yorumlar alınamadı. Yazı: {BlogId}", blogId);
            return new CommentThreadViewModel { BlogId = blogId };
        }
    }

    public Task<CommentOutcome> SubmitAsync(CommentFormModel form, CancellationToken ct = default)
        => PostAsync("", new
        {
            blogId = form.BlogId,
            parentId = form.ParentId,
            authorName = form.AuthorName,
            authorEmail = form.AuthorEmail,
            body = form.Body,
            notifyOnReply = form.NotifyOnReply,
            turnstileToken = form.TurnstileToken
        }, "Yorum gönderilemedi.", ct);

    public Task<CommentOutcome> DisableNotificationsAsync(string token, CancellationToken ct = default)
        => PostAsync("disable-notifications", new { token }, "Yorum bildirimleri kapatılamadı.", ct);

    private async Task<CommentOutcome> PostAsync(string path, object body, string failureLog, CancellationToken ct)
    {
        try
        {
            var url = string.IsNullOrEmpty(path) ? Base : $"{Base}/{path}";
            var response = await _httpClient.PostAsJsonAsync(url, body, ct);
            var envelope = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions, ct);

            if (envelope is null)
                return new CommentOutcome(response.IsSuccessStatusCode, null);

            return new CommentOutcome(envelope.Success, envelope.Success ? envelope.Message : envelope.Errors.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Message}", failureLog);
            return new CommentOutcome(false, null);
        }
    }
}
