using FurkanTural_Admin.Helpers;
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

        var vm = await BuildViewModelAsync(token, title, activeFilter, deletedFilter, dateFrom, dateTo, blogId, pageNumber, pageSize, cancellationToken);
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

        var vm = await BuildViewModelAsync(token, title, activeFilter, deletedFilter, dateFrom, dateTo, blogId, pageNumber, pageSize, cancellationToken);
        return PartialView("_BlogTable", vm);
    }

    private async Task<BlogIndexViewModel> BuildViewModelAsync(
        string token,
        string? title, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo, int? blogId,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(title, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize)
            .With("blogId", blogId);

        var countsTask = _blogApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _blogApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        var categoriesTask = _categoryApiClient.GetAllForAdminAsync(token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask, categoriesTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;
        var availableCategories = (await categoriesTask).Where(c => c.IsActive && !c.IsDeleted).ToList();

        return new BlogIndexViewModel
        {
            Rows          = rows,
            TotalCount    = counts?.Total ?? 0,
            ActiveCount   = counts?.Active ?? 0,
            PassiveCount  = counts?.Passive ?? 0,
            DeletedCount  = counts?.Deleted ?? 0,
            SearchTitle   = title,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            BlogIdFilter  = blogId,
            PageNumber    = request.PageNumber,
            PageSize      = request.PageSize,
            TotalFiltered = totalFiltered,
            AvailableCategories = availableCategories
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

    [HttpGet]
    public async Task<IActionResult> BlogOptions(string? search, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var options = await _blogApiClient.GetAdminOptionsAsync(search, null, token, cancellationToken);
        return Json(options.Select(o => new { value = o.Id, label = o.Label ?? "" }));
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