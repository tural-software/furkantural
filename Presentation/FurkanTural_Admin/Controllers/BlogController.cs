using FurkanTural_Admin.Models.Blog;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class BlogController(IBlogApiClient blogApiClient, ICategoryApiClient categoryApiClient) : Controller
{
    private readonly IBlogApiClient _blogApiClient = blogApiClient;
    private readonly ICategoryApiClient _categoryApiClient = categoryApiClient;

    public async Task<IActionResult> Index(
        string? title,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int? blogId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _blogApiClient.GetAllForAdminAsync(token, cancellationToken);
        var categories = await _categoryApiClient.GetAllForAdminAsync(token, cancellationToken);
        var availableCategories = categories.Where(c => c.IsActive && !c.IsDeleted).ToList();

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(title))
            filtered = filtered.Where(b => b.Title != null && b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(b => b.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(b => !b.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(b => b.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(b => !b.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(b => b.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(b => b.CreatedAt < to.AddDays(1));

        if (blogId.HasValue)
            filtered = filtered.Where(b => b.Id == blogId.Value);

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new BlogIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(b => b.IsActive && !b.IsDeleted),
            PassiveCount  = all.Count(b => !b.IsActive && !b.IsDeleted),
            DeletedCount  = all.Count(b => b.IsDeleted),
            SearchTitle   = title,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            BlogIdFilter  = blogId,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered,
            AvailableCategories = availableCategories
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? title,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int? blogId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _blogApiClient.GetAllForAdminAsync(token, cancellationToken);

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(title))
            filtered = filtered.Where(b => b.Title != null && b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(b => b.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(b => !b.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(b => b.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(b => !b.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(b => b.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(b => b.CreatedAt < to.AddDays(1));

        if (blogId.HasValue)
            filtered = filtered.Where(b => b.Id == blogId.Value);

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new BlogIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(b => b.IsActive && !b.IsDeleted),
            PassiveCount  = all.Count(b => !b.IsActive && !b.IsDeleted),
            DeletedCount  = all.Count(b => b.IsDeleted),
            SearchTitle   = title,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            BlogIdFilter  = blogId,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };

        return PartialView("_BlogTable", vm);
    }

    public IActionResult TableDetail()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        return View();
    }

    // ── Blog options (filter dropdown) ───────────────────────────────────────
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

        var ok = await _blogApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _blogApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _blogApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] BlogFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (dto.Title != null && dto.Title.Length > 500)
            return BadRequest(new { message = "Başlık en fazla 500 karakter olabilir." });

        var ok = await _blogApiClient.CreateAsync(dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] BlogFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (dto.Title != null && dto.Title.Length > 500)
            return BadRequest(new { message = "Başlık en fazla 500 karakter olabilir." });

        var ok = await _blogApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt güncellenirken bir hata oluştu." });
    }
}
