using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FurkanTural_Blog.Helpers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;

namespace FurkanTural_Blog.Controllers;

public class HomeController(IBlogApiService blogApi, IConfiguration configuration) : Controller
{
    private readonly IBlogApiService _blogApi = blogApi;
    private readonly string _apiBase = (configuration["Api:BaseUrl"] ?? string.Empty).TrimEnd('/');

    /// <summary>Değer keyfi değil: kart ızgarası kapsayıcı genişliğine göre bir ilâ dört sütun çiziyor ve 12 dördüne de tam bölündüğü için hiçbir kırılma noktasında yarım satır kalmıyor. Izgaranın sütun sayısı değişirse bu sayı da yeniden seçilmelidir.</summary>
    private const int PageSize = 12;

    /// <summary>Aralık dışı sayfa numarası hata değil, son geçerli sayfaya yönlendirme üretir ve filtreler korunur; elle yazılmış bir adres kullanıcıyı boş listeyle baş başa bırakmaz.<para>Kapak görselleri yalnızca bu sayfadaki yazılar için ve paralel çekilir. Çağrı sayısı böylece sayfa boyutunu hiç aşmaz ve arşiv büyüdükçe artmaz.</para><para>Kategori ve arama artık kendi adreslerinde yaşıyor. Buraya eski sorgu dizesiyle gelen istek kalıcı olarak oraya yönlendirilir: iki adres aynı listeyi gösterirse arama motoru hangisinin kanonik olduğunu bilemez.</para></summary>
    public async Task<IActionResult> Index(int page = 1, int? categoryId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (categoryId is int legacyCategory)
        {
            var categories = await _blogApi.GetCategoriesAsync(cancellationToken);
            var match = categories.FirstOrDefault(c => c.Id == legacyCategory);
            if (match is not null && match.Slug.Length > 0)
                return RedirectToActionPermanent(nameof(Category), new { slug = match.Slug, page = page > 1 ? page : (int?)null });
        }

        if (!string.IsNullOrWhiteSpace(search))
            return RedirectToActionPermanent(nameof(Search), new { q = search.Trim(), page = page > 1 ? page : (int?)null });

        return await ListAsync(BlogListKind.Home, page, null, null, null, cancellationToken);
    }

    /// <summary>Kategori sayfası. Adres kategori adından üretilen slug'dır; eşleştirme de aynı dönüşümden geçer.<para>İki sınırı vardır ve ikisi de şemada slug sütunu olmamasından gelir: kategori adı değişirse eski adres 404 verir, iki ad aynı slug'a düşerse (<c>C#</c> ile <c>C</c> gibi) biri erişilemez kalır. Bugün on kategorinin onu da ayrı slug üretiyor. Eşleşme kimliğe göre sıralanır, böylece hangisinin kazandığı hiç değilse kararlıdır — API'nin döndürme sırasına göre değişmez.</para></summary>
    [Route("kategori/{slug}", Name = "BlogCategory")]
    public async Task<IActionResult> Category(string slug, int page = 1, CancellationToken cancellationToken = default)
    {
        var categories = await _blogApi.GetCategoriesAsync(cancellationToken);
        var category = categories
            .OrderBy(c => c.Id)
            .FirstOrDefault(c => string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (category is null)
            return NotFound();

        return await ListAsync(BlogListKind.Category, page, category.Id, null, category, cancellationToken);
    }

    /// <summary>Arama sonuçları. Sorgu boşsa liste değil boş durum gösterilir; boş aramayı tüm yazılara çevirmek, okuru aradığını bulmuş sanmasına yol açar.</summary>
    [Route("ara", Name = "BlogSearch")]
    public Task<IActionResult> Search(string? q = null, int page = 1, CancellationToken cancellationToken = default)
        => ListAsync(BlogListKind.Search, page, null, q?.Trim(), null, cancellationToken);

    [Route("hakkinda", Name = "BlogAbout")]
    public async Task<IActionResult> About(CancellationToken cancellationToken = default)
    {
        var latest = await _blogApi.GetPostsPagedAsync(1, 5, null, null, cancellationToken);
        return View(latest);
    }

    private async Task<IActionResult> ListAsync(
        BlogListKind kind, int page, int? categoryId, string? search, CategoryViewModel? activeCategory, CancellationToken cancellationToken)
    {
        if (page < 1) page = 1;

        var paged = await _blogApi.GetPostsPagedAsync(page, PageSize, categoryId, search, cancellationToken);
        paged.Kind = kind;
        paged.ActiveCategory = activeCategory;

        if (paged.TotalPages > 0 && page > paged.TotalPages)
            return RedirectToRoute(RouteNameFor(kind), RouteValuesFor(paged, paged.TotalPages));

        await AttachCoversAsync(paged, cancellationToken);
        return View(ViewNameFor(kind), paged);
    }

    private static string RouteNameFor(BlogListKind kind) => kind switch
    {
        BlogListKind.Category => "BlogCategory",
        BlogListKind.Search => "BlogSearch",
        _ => "default"
    };

    private static object RouteValuesFor(PagedPostsViewModel model, int page) => model.Kind switch
    {
        BlogListKind.Category => new { slug = model.ActiveCategory?.Slug, page },
        BlogListKind.Search => new { q = model.Search, page },
        _ => new { controller = "Home", action = "Index", page }
    };

    private static string ViewNameFor(BlogListKind kind) => kind switch
    {
        BlogListKind.Category => "Category",
        BlogListKind.Search => "Search",
        _ => "Index"
    };

    /// <summary>Kapaklar listeye sonradan iliştirilir; API tek çağrıda vermiyor. Yalnızca ekrandaki yazılar için ve paralel çekilir, dolayısıyla çağrı sayısı sayfa boyutunu aşmaz.</summary>
    private async Task AttachCoversAsync(PagedPostsViewModel paged, CancellationToken cancellationToken)
    {
        if (paged.Items.Count == 0) return;

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

    /// <summary>Bölü işareti taşıyan değer göreli yoldur ve olduğu gibi eklenir; taşımayan değer klasörlere ayrılmadan önceki düzenden kalma düz dosya adıdır ve eski yükleme klasörü altında aranır. Ayrımın kaynağı API tarafındaki dosya servisidir, ikisi birlikte değişmelidir.</summary>
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
