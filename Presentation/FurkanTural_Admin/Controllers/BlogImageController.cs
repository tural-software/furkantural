using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.BlogImage;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class BlogImageController(IBlogImageApiClient blogImageApiClient, IBlogApiClient blogApiClient, IConfiguration configuration) : Controller
{
    private readonly IBlogImageApiClient _blogImageApiClient = blogImageApiClient;
    private readonly IBlogApiClient _blogApiClient = blogApiClient;
    private readonly string _apiBaseUrl = configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty;

    private static BlogImageIndexViewModel BuildViewModel(
        IReadOnlyList<BlogImageAdminDto> all,
        string? searchUrl,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? blogIdFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber,
        int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchUrl))
            filtered = filtered.Where(b => b.Url != null && b.Url.Contains(searchUrl, StringComparison.OrdinalIgnoreCase));

        if (isCoverFilter == "cover")
            filtered = filtered.Where(b => b.IsCover);
        else if (isCoverFilter == "notCover")
            filtered = filtered.Where(b => !b.IsCover);

        if (activeFilter == "active")
            filtered = filtered.Where(b => b.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(b => !b.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(b => b.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(b => !b.IsDeleted);

        if (blogIdFilter.HasValue)
            filtered = filtered.Where(b => b.BlogId == blogIdFilter.Value);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(b => b.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(b => b.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new BlogImageIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(b => b.IsActive && !b.IsDeleted),
            PassiveCount  = all.Count(b => !b.IsActive && !b.IsDeleted),
            DeletedCount  = all.Count(b => b.IsDeleted),
            SearchUrl     = searchUrl,
            IsCoverFilter = isCoverFilter,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            BlogIdFilter  = blogIdFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };
    }

    public async Task<IActionResult> Index(
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? blogId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _blogImageApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, url, isCoverFilter, activeFilter, deletedFilter, blogId, dateFrom, dateTo, pageNumber, pageSize);

        ViewData["ApiBaseUrl"] = _apiBaseUrl;
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? blogId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _blogImageApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, url, isCoverFilter, activeFilter, deletedFilter, blogId, dateFrom, dateTo, pageNumber, pageSize);

        return PartialView("_BlogImageTable", vm);
    }

    public async Task<IActionResult> TableDetail([FromServices] ISchemaApiClient schemaApiClient, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var vm = await TableSchemaBuilder.BuildAsync(
            schemaApiClient, Url, ControllerContext.ActionDescriptor.ControllerName, token, cancellationToken);

        return View("TableSchema", vm);
    }
    [HttpGet]
    public async Task<IActionResult> BlogOptions(CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var blogs = await _blogApiClient.GetAllForAdminAsync(token, cancellationToken);
        var options = blogs
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.Title)
            .Select(b => new { value = b.Id, label = b.Title ?? $"Blog #{b.Id}" })
            .ToList();

        return Json(options);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _blogImageApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _blogImageApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _blogImageApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        IFormFile? imageFile,
        string? altText,
        bool isCover = false,
        int blogId = 0,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (imageFile == null || imageFile.Length == 0)
            return BadRequest(new { message = "Görsel dosyası zorunludur." });

        if (blogId <= 0)
            return BadRequest(new { message = "Geçerli bir Blog ID giriniz." });

        if (altText != null && altText.Length > 500)
            return BadRequest(new { message = "Açıklama metni en fazla 500 karakter olabilir." });

        var newId = await _blogImageApiClient.CreateAsync(imageFile, altText, isCover, blogId, token, cancellationToken);
        if (!newId.HasValue)
            return StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        IFormFile? imageFile,
        string? altText,
        bool isCover = false,
        int blogId = 0,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (blogId <= 0)
            return BadRequest(new { message = "Geçerli bir Blog ID giriniz." });

        if (altText != null && altText.Length > 500)
            return BadRequest(new { message = "Açıklama metni en fazla 500 karakter olabilir." });

        var ok = await _blogImageApiClient.UpdateAsync(id, imageFile, altText, isCover, blogId, token, cancellationToken);
        if (!ok)
            return StatusCode(500, new { message = "Güncelleme işlemi başarısız oldu." });

        return Ok();
    }
}