using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Category;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class CategoryController(ICategoryApiClient categoryApiClient) : Controller
{
    private readonly ICategoryApiClient _categoryApiClient = categoryApiClient;

    public async Task<IActionResult> Index(
        string? name,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var vm = await BuildViewModelAsync(token, name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? name,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var vm = await BuildViewModelAsync(token, name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_CategoryTable", vm);
    }

    private async Task<CategoryIndexViewModel> BuildViewModelAsync(
        string token,
        string? name,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);

        var countsTask = _categoryApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _categoryApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new CategoryIndexViewModel
        {
            Rows          = rows,
            TotalCount    = counts?.Total ?? 0,
            ActiveCount   = counts?.Active ?? 0,
            PassiveCount  = counts?.Passive ?? 0,
            DeletedCount  = counts?.Deleted ?? 0,
            SearchName    = name,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = request.PageNumber,
            PageSize      = request.PageSize,
            TotalFiltered = totalFiltered
        };
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