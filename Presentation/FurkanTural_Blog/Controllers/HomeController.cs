using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;

namespace FurkanTural_Blog.Controllers;

public class HomeController(IBlogApiService blogApi, IConfiguration configuration) : Controller
{
    private readonly IBlogApiService _blogApi = blogApi;
    private readonly string _apiBase = (configuration["Api:BaseUrl"] ?? string.Empty).TrimEnd('/');

    /// <summary>
    /// Değer keyfi değil: kart ızgarası kapsayıcı genişliğine göre bir ilâ dört sütun çiziyor ve 12
    /// dördüne de tam bölündüğü için hiçbir kırılma noktasında yarım satır kalmıyor. Izgaranın
    /// sütun sayısı değişirse bu sayı da yeniden seçilmelidir.
    /// </summary>
    private const int PageSize = 12;

    /// <summary>
    /// Aralık dışı sayfa numarası hata değil, son geçerli sayfaya yönlendirme üretir ve filtreler
    /// korunur; elle yazılmış bir adres kullanıcıyı boş listeyle baş başa bırakmaz.
    ///
    /// Kapak görselleri yalnızca bu sayfadaki yazılar için ve paralel çekilir. Çağrı sayısı böylece
    /// sayfa boyutunu hiç aşmaz ve arşiv büyüdükçe artmaz.
    /// </summary>
    public async Task<IActionResult> Index(int page = 1, int? categoryId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;

        var paged = await _blogApi.GetPostsPagedAsync(page, PageSize, categoryId, search, cancellationToken);

        if (paged.TotalPages > 0 && page > paged.TotalPages)
            return RedirectToAction(nameof(Index), new { page = paged.TotalPages, categoryId, search });

        if (paged.Items.Count > 0)
        {
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

    /// <summary>
    /// Bölü işareti taşıyan değer göreli yoldur ve olduğu gibi eklenir; taşımayan değer klasörlere
    /// ayrılmadan önceki düzenden kalma düz dosya adıdır ve eski yükleme klasörü altında aranır.
    /// Ayrımın kaynağı API tarafındaki dosya servisidir, ikisi birlikte değişmelidir.
    /// </summary>
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