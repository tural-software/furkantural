using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;

namespace FurkanTural_Blog.Controllers;

public class HomeController(IBlogApiService blogApi, IConfiguration configuration) : Controller
{
    private readonly IBlogApiService _blogApi = blogApi;
    private readonly string _apiBase = (configuration["Api:BaseUrl"] ?? string.Empty).TrimEnd('/');

    // Liste sayfa boyutu — 1000+ yazıda bile DB yalnız bu kadar satır döndürür (API tarafı sayfalar).
    private const int PageSize = 9;

    public async Task<IActionResult> Index(int page = 1, int? categoryId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;

        var paged = await _blogApi.GetPostsPagedAsync(page, PageSize, categoryId, search, cancellationToken);

        // İstenen sayfa aralık dışındaysa (örn. ?page=9999) son geçerli sayfaya yönlendir (filtre korunur).
        if (paged.TotalPages > 0 && page > paged.TotalPages)
            return RedirectToAction(nameof(Index), new { page = paged.TotalPages, categoryId, search });

        if (paged.Items.Count > 0)
        {
            // Yalnız BU sayfadaki yazıların kapaklarını paralel çek → çağrı sayısı her zaman ≤ PageSize,
            // toplam yazı sayısından bağımsız (eski "tüm görseller" çağrısı 1000+ yazıda ölçeklenmezdi).
            var covers = await Task.WhenAll(paged.Items.Select(async post =>
            {
                var images = await _blogApi.GetImagesByBlogAsync(post.Id, cancellationToken);
                var cover = images.FirstOrDefault(i => i.IsCover) ?? images.FirstOrDefault();
                return (post.Id, cover);
            }));

            var coverById = covers.ToDictionary(x => x.Id, x => x.cover);
            foreach (var post in paged.Items)
            {
                if (coverById.TryGetValue(post.Id, out var cover) && cover is not null && !string.IsNullOrWhiteSpace(cover.Url))
                {
                    post.CoverImageUrl = BuildImageUrl(cover.Url);
                    post.CoverAltText = cover.AltText;
                }
            }
        }

        return View(paged);
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
