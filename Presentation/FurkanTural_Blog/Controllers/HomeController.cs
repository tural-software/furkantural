using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FurkanTural_Blog.Helpers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;

namespace FurkanTural_Blog.Controllers;

public class HomeController(
    IBlogApiService blogApi,
    ICommentClient commentClient,
    IAppConfigService appConfig,
    IConfiguration configuration) : Controller
{
    private readonly IBlogApiService _blogApi = blogApi;
    private readonly ICommentClient _commentClient = commentClient;
    private readonly IAppConfigService _appConfig = appConfig;
    private readonly string _apiBase = (configuration["Api:BaseUrl"] ?? string.Empty).TrimEnd('/');

    /// <summary>Değer keyfi değil: kart ızgarası kapsayıcı genişliğine göre bir ilâ dört sütun çiziyor ve 12 dördüne de tam bölündüğü için hiçbir kırılma noktasında yarım satır kalmıyor. Izgaranın sütun sayısı değişirse bu sayı da yeniden seçilmelidir.</summary>
    private const int PageSize = 12;

    /// <summary>Arşiv sayfasındaki etiket bulutunun tavanı. Bulut bir gezinme aracıdır, envanter değil: her etiketi göstermek okuru seçim yapamayacağı bir duvarla karşılaştırır.</summary>
    private const int TagCloudSize = 24;

    /// <summary>Gönderim sonrası adres satırında taşınan işaret. Sonucu oturuma yazmak yerine adreste taşımak, hem çerez gerektirmez hem de yenilenen sayfada onay metninin kaybolmasını doğal kılar.</summary>
    private const string SubmittedFlag = "alindi";

    private const string SubmittedMessage = "Yorumunuz alındı. Onaylandıktan sonra sayfada görünecek.";

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

        return await ListAsync(BlogListKind.Home, page, null, null, null, null, cancellationToken);
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

        return await ListAsync(BlogListKind.Category, page, category.Id, null, null, category, cancellationToken);
    }

    /// <summary>Etiket sayfası. Kategori sayfasından tek farkı eşleştirmenin nerede yapıldığıdır: kategoriler zaten filtre çubuğu için tümüyle çekildiğinden orada bellekte aranır, etiketler ise çok daha kalabalık olacağı için tek adres sorgusuyla API'den istenir.<para>Bulunamayan slug 404 döner; var olmayan bir etiketin tüm yazıları göstermesi, yanlış bağlantıyı sessizce doğru göstermek olurdu.</para></summary>
    [Route("etiket/{slug}", Name = "BlogTag")]
    public async Task<IActionResult> Tag(string slug, int page = 1, CancellationToken cancellationToken = default)
    {
        var tag = await _blogApi.GetTagBySlugAsync(slug, cancellationToken);
        if (tag is null)
            return NotFound();

        return await ListAsync(BlogListKind.Tag, page, null, tag, null, null, cancellationToken);
    }

    /// <summary>Arama sonuçları. Sorgu boşsa liste değil boş durum gösterilir; boş aramayı tüm yazılara çevirmek, okuru aradığını bulmuş sanmasına yol açar.</summary>
    [Route("ara", Name = "BlogSearch")]
    public Task<IActionResult> Search(string? q = null, int page = 1, CancellationToken cancellationToken = default)
        => ListAsync(BlogListKind.Search, page, null, null, q?.Trim(), null, cancellationToken);

    [Route("hakkinda", Name = "BlogAbout")]
    public async Task<IActionResult> About(CancellationToken cancellationToken = default)
    {
        var latest = await _blogApi.GetPostsPagedAsync(1, 5, null, null, null, cancellationToken);
        return View(latest);
    }

    /// <summary>Arşiv: yıl/ay gruplu tam liste, sayfalama yok. Tek çağrı bütün arşivi getirir, çünkü kaynak uç yalnızca kimlik, başlık ve tarih taşır — gövde veri tabanından hiç çıkmaz.<para>Bu, veri çekim kuralının "belirli bir sebeple tam çekim" istisnasıdır: arşivin işi tek ekranda taranmaktır, sayfalanmış bir arşiv arşiv olmaktan çıkar. Sayfa büyüdüğünde sınırlayan şey satır sayısı değil satır boyudur.</para></summary>
    [Route("arsiv", Name = "BlogArchive")]
    public async Task<IActionResult> Archive(CancellationToken cancellationToken = default)
    {
        var archiveTask = _blogApi.GetArchiveAsync(cancellationToken);
        var tagsTask = _blogApi.GetPopularTagsAsync(TagCloudSize, cancellationToken);
        await Task.WhenAll(archiveTask, tagsTask);

        var archive = await archiveTask;
        archive.Tags = await tagsTask;
        return View(archive);
    }

    private async Task<IActionResult> ListAsync(
        BlogListKind kind, int page, int? categoryId, TagViewModel? activeTag, string? search, CategoryViewModel? activeCategory, CancellationToken cancellationToken)
    {
        if (page < 1) page = 1;

        var paged = await _blogApi.GetPostsPagedAsync(page, PageSize, categoryId, activeTag?.Id, search, cancellationToken);
        paged.Kind = kind;
        paged.ActiveCategory = activeCategory;
        paged.ActiveTag = activeTag;

        if (paged.TotalPages > 0 && page > paged.TotalPages)
            return RedirectToRoute(RouteNameFor(kind), RouteValuesFor(paged, paged.TotalPages));

        await AttachCoversAsync(paged, cancellationToken);
        return View(ViewNameFor(kind), paged);
    }

    private static string RouteNameFor(BlogListKind kind) => kind switch
    {
        BlogListKind.Category => "BlogCategory",
        BlogListKind.Tag => "BlogTag",
        BlogListKind.Search => "BlogSearch",
        _ => "default"
    };

    private static object RouteValuesFor(PagedPostsViewModel model, int page) => model.Kind switch
    {
        BlogListKind.Category => new { slug = model.ActiveCategory?.Slug, page },
        BlogListKind.Tag => new { slug = model.ActiveTag?.Slug, page },
        BlogListKind.Search => new { q = model.Search, page },
        _ => new { controller = "Home", action = "Index", page }
    };

    private static string ViewNameFor(BlogListKind kind) => kind switch
    {
        BlogListKind.Category => "Category",
        BlogListKind.Tag => "Tag",
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

    /// <summary>Eski kimlik adresi. Yazının kanonik adresi artık <c>/yazi/{slug}</c>; buraya gelen istek kalıcı olarak oraya taşınır.<para>Kalıcı yönlendirme şart: geçici olsaydı arama motoru eski adresi tutmaya devam eder ve aynı yazı iki adresten görünürdü. Yazının kendisi burada çizilmez, yalnızca slug'ı okunacak kadar çekilir.</para><para>Slug boşsa yönlendirme yapılmaz ve yazı buradan çizilir; adressiz bir yönlendirme okuru hiçbir yere götürmezdi.</para></summary>
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        var post = await _blogApi.GetPostAsync(id, cancellationToken);
        if (post is null)
            return NotFound();

        if (post.Slug.Length > 0)
            return RedirectToRoutePermanent("BlogPost", new { slug = post.Slug });

        await AttachDetailAsync(post, cancellationToken);
        return View(nameof(Post), post);
    }

    /// <summary>Yazının kanonik adresi. Slug kalıcıdır: başlık değişse de adres durur, dolayısıyla paylaşılmış bağlantılar kırılmaz.</summary>
    [Route("yazi/{slug}", Name = "BlogPost")]
    public async Task<IActionResult> Detail(string slug, string? yorum = null, CancellationToken cancellationToken = default)
    {
        var post = await _blogApi.GetPostBySlugAsync(slug, cancellationToken);
        if (post is null)
            return NotFound();

        await AttachDetailAsync(post, cancellationToken);

        if (yorum == SubmittedFlag)
        {
            post.CommentForm.Submitted = true;
            post.CommentForm.Succeeded = true;
            post.CommentForm.ResultMessage = SubmittedMessage;
        }

        return View(nameof(Post), post);
    }

    /// <summary>Yorum gönderimi. Yazı sayfasının kendi denetleyicisinde durur, çünkü başarısızlıkta çizilmesi gereken şey o sayfanın tamamıdır: doğrulama hatası kullanıcıyı yazdığı metinden etmemeli, form aynı yerde ve dolu hâlde geri gelmelidir.<para>Başarıda ise yönlendirme yapılır ve sonuç adres satırındaki bir işaretle taşınır. Bülten sayfasından ayrıldığı tek yer budur: orada sayfada kalmanın bedeli yeniden gönderim uyarısıdır, burada tarayıcının yenile tuşu ikinci bir yorum kaydı açardı.</para><para>Tuzak alan doluysa istek API'ye hiç çıkmaz ama ekranda başarı görünür; gerekçesi bülten formuyla aynıdır.</para></summary>
    [HttpPost]
    [Route("yazi/{slug}/yorum", Name = "BlogCommentSubmit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(string slug, CommentFormModel form, CancellationToken cancellationToken)
    {
        var post = await _blogApi.GetPostBySlugAsync(slug, cancellationToken);
        if (post is null)
            return NotFound();

        var scripted = IsScriptedRequest();

        if (!string.IsNullOrWhiteSpace(form.Website))
            return scripted
                ? Json(new { ok = true, message = SubmittedMessage })
                : RedirectToRoute("BlogPost", new { slug, yorum = SubmittedFlag });

        form.BlogId = post.Id;
        form.Submitted = true;
        form.Succeeded = false;

        string? failure = null;

        if (ModelState.IsValid)
        {
            if (string.IsNullOrWhiteSpace(form.TurnstileToken))
            {
                failure = "Bot doğrulaması tamamlanmadı. Lütfen tekrar deneyin.";
            }
            else
            {
                var outcome = await _commentClient.SubmitAsync(form, cancellationToken);
                if (outcome.Succeeded)
                    return scripted
                        ? Json(new { ok = true, message = SubmittedMessage })
                        : RedirectToRoute("BlogPost", new { slug, yorum = SubmittedFlag });

                failure = string.IsNullOrWhiteSpace(outcome.Message)
                    ? "Yorum şu anda alınamıyor. Kısa süre sonra tekrar deneyin."
                    : outcome.Message;
            }
        }

        if (scripted)
            return Json(new { ok = false, message = failure, errors = FieldErrors() });

        form.ResultMessage = failure;
        await AttachDetailAsync(post, cancellationToken);
        post.CommentForm = form;

        return View(nameof(Post), post);
    }

    private bool IsScriptedRequest() =>
        HttpContext is not null
        && string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private Dictionary<string, string> FieldErrors() =>
        ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors[0].ErrorMessage);

    /// <summary>Okunma sayacını artırır. Sayfanın kendisi çizilirken artırılmaz: JavaScript çalıştırmayan gezginler böylece sayıya girmez ve aynı okuru günde bir kez saymanın işareti tarayıcıda durur.<para>Yanıt daima boştur ve daima 204'tür. Sayacın artıp artmadığını ele veren bir yanıt, ucu "bu yazı sayıldı mı" diye yoklanabilir hâle getirir; sayfanın da bu bilgiye ihtiyacı yok.</para></summary>
    [HttpPost]
    [Route("okuma/{id:int}", Name = "BlogPostView")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterView(int id, CancellationToken cancellationToken)
    {
        await _blogApi.RegisterViewAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Kapak ile ilgili yazıları yazıya iliştirir. Görseller yalnızca kimliğe bağlı olduğu için hemen istenir; ilgili yazılar ancak kategoriler bilindikten sonra istenebilir, dolayısıyla o çağrı sonra gelir.</summary>
    private async Task AttachDetailAsync(BlogPostViewModel post, CancellationToken cancellationToken)
    {
        var images = await _blogApi.GetImagesByBlogAsync(post.Id, cancellationToken);
        var cover = images.FirstOrDefault(i => i.IsCover) ?? images.FirstOrDefault();
        if (cover is not null && !string.IsNullOrWhiteSpace(cover.Url))
        {
            post.CoverImageUrl = BuildImageUrl(cover.Url);
            post.CoverAltText = cover.AltText;
        }

        var comments = _commentClient.GetThreadAsync(post.Id, cancellationToken);
        var siteKey = _appConfig.GetTurnstileSiteKeyAsync(cancellationToken);
        await AttachRelatedAsync(post, cancellationToken);

        post.Comments = await comments;
        post.TurnstileSiteKey = await siteKey;
        post.CommentForm.BlogId = post.Id;
    }

    /// <summary>Adaylar yazının ilk kategorisinden tek sayfada çekilir. Kategori başına ayrı çağrı yapılmaz: her sayfalı çağrı kendi içinde kategori sözlüğünü de istediği için maliyet çağrı sayısının iki katıdır ve üç kart için orantısız kalır. Sıralama yine de yazının bütün kategorilerine bakar, çünkü dönen adaylar kendi kategorilerini taşır.</summary>
    private async Task AttachRelatedAsync(BlogPostViewModel post, CancellationToken cancellationToken)
    {
        var primaryCategory = post.Categories.Select(c => c.Id).FirstOrDefault();
        if (primaryCategory == 0)
            return;

        var page = await _blogApi.GetPostsPagedAsync(
            1, RelatedPosts.CandidatePageSize, primaryCategory, null, null, cancellationToken);

        post.Related = RelatedPosts.Pick(post, page.Items);
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
