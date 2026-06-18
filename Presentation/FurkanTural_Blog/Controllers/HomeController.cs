using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;

namespace FurkanTural_Blog.Controllers;

public class HomeController(IBlogApiService blogApi, IConfiguration configuration) : Controller
{
    private readonly IBlogApiService _blogApi = blogApi;
    private readonly string _apiBase = (configuration["Api:BaseUrl"] ?? string.Empty).TrimEnd('/');

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var posts = await _blogApi.GetPostsAsync(cancellationToken);
        if (posts.Count > 0)
        {
            // Kapakları tek çağrıyla çek (N+1 yok); blog başına kapak (IsCover öncelikli) eşle.
            var images = await _blogApi.GetAllImagesAsync(cancellationToken);
            var coverByBlog = images
                .Where(i => !string.IsNullOrWhiteSpace(i.Url))
                .GroupBy(i => i.BlogId)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault(x => x.IsCover) ?? g.First());

            foreach (var post in posts)
            {
                if (coverByBlog.TryGetValue(post.Id, out var cover))
                {
                    post.CoverImageUrl = BuildImageUrl(cover.Url!);
                    post.CoverAltText = cover.AltText;
                }
            }
        }
        return View(posts);
    }

    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        var post = await _blogApi.GetPostAsync(id, cancellationToken);
        if (post is null)
            return NotFound();

        var images = await _blogApi.GetImagesByBlogAsync(id, cancellationToken);
        var cover = images.FirstOrDefault(i => i.IsCover) ?? images.FirstOrDefault();
        if (cover is not null && !string.IsNullOrWhiteSpace(cover.Url))
        {
            post.CoverImageUrl = BuildImageUrl(cover.Url);
            post.CoverAltText = cover.AltText;
        }
        return View(post);
    }

    // Görsel public adresi: yeni kayıtlar göreli yol taşır ('blogs/images/..'),
    // eski (legacy) kayıtlar düz dosya adı → 'images/uploads/' altında servis edilir.
    private string BuildImageUrl(string url) =>
        url.Contains('/') ? $"{_apiBase}/{url}" : $"{_apiBase}/images/uploads/{url}";

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        });
    }
}
