using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Category;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class CategoryController(ICategoryApiClient categoryApiClient) : Controller
{
    private readonly ICategoryApiClient _categoryApiClient = categoryApiClient;

    private static CategoryIndexViewModel BuildViewModel(
        IReadOnlyList<CategoryAdminDto> all,
        string? name, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(name))
            filtered = filtered.Where(c => c.Name != null && c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(c => c.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(c => !c.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(c => c.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(c => !c.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(c => c.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(c => c.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();

        var safePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new CategoryIndexViewModel
        {
            Rows = rows,
            TotalCount = all.Count,
            ActiveCount = all.Count(c => c.IsActive && !c.IsDeleted),
            PassiveCount = all.Count(c => !c.IsActive && !c.IsDeleted),
            DeletedCount = all.Count(c => c.IsDeleted),
            SearchName = name,
            ActiveFilter = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalFiltered = filteredList.Count
        };
    }

    public async Task<IActionResult> Index(
        string? name, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _categoryApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? name, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _categoryApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);
        return PartialView("_CategoryTable", vm);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _categoryApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _categoryApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _categoryApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CategoryFormDto dto, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Kategori adı boş olamaz." });

        var ok = await _categoryApiClient.CreateAsync(dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [FromForm] CategoryFormDto dto, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Kategori adı boş olamaz." });

        var ok = await _categoryApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt güncellenirken bir hata oluştu." });
    }
}