using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.MailTemplate;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class MailTemplateController(IMailTemplateApiClient mailTemplateApiClient) : Controller
{
    private readonly IMailTemplateApiClient _mailTemplateApiClient = mailTemplateApiClient;

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

        var all = await _mailTemplateApiClient.GetAllForAdminAsync(token, cancellationToken);
        var types = await _mailTemplateApiClient.GetTypesAsync(token, cancellationToken);
        var appSources = await _mailTemplateApiClient.GetAppSourcesAsync(token, cancellationToken);
        var vm = BuildViewModel(all, types, appSources, name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);
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

        var all = await _mailTemplateApiClient.GetAllForAdminAsync(token, cancellationToken);
        var types = await _mailTemplateApiClient.GetTypesAsync(token, cancellationToken);
        var appSources = await _mailTemplateApiClient.GetAppSourcesAsync(token, cancellationToken);
        var vm = BuildViewModel(all, types, appSources, name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);
        return PartialView("_MailTemplateTable", vm);
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
    public async Task<IActionResult> Create(
        [FromForm] MailTemplateFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _mailTemplateApiClient.CreateAsync(dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] MailTemplateFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _mailTemplateApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Güncelleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _mailTemplateApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _mailTemplateApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpGet]
    public async Task<IActionResult> PreviewHtml(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var html = await _mailTemplateApiClient.GetHtmlContentAsync(id, token, cancellationToken);
        if (html is null)
            return NotFound(new { message = "HTML içeriği bulunamadı." });

        return Json(new { htmlContent = html });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _mailTemplateApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    private static MailTemplateIndexViewModel BuildViewModel(
        IReadOnlyList<MailTemplateAdminDto> all,
        IReadOnlyList<MailTemplateTypeOptionDto> types,
        IReadOnlyList<AppSourceOptionDto> appSources,
        string? name, string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(name))
            filtered = filtered.Where(s => s.Name != null && s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(s => s.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(s => !s.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(s => s.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(s => !s.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(s => s.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(s => s.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new MailTemplateIndexViewModel
        {
            Rows = rows,
            Types = types,
            AppSources = appSources,
            TotalCount = all.Count,
            ActiveCount = all.Count(s => s.IsActive && !s.IsDeleted),
            PassiveCount = all.Count(s => !s.IsActive && !s.IsDeleted),
            DeletedCount = all.Count(s => s.IsDeleted),
            SearchName = name,
            ActiveFilter = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalFiltered = totalFiltered
        };
    }
}