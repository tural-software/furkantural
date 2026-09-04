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
        return PartialView("_MailTemplateTable", vm);
    }

    private async Task<MailTemplateIndexViewModel> BuildViewModelAsync(
        string token,
        string? name, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);

        var countsTask = _mailTemplateApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _mailTemplateApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        var typesTask = _mailTemplateApiClient.GetTypesAsync(token, cancellationToken);
        var appSourcesTask = _mailTemplateApiClient.GetAppSourcesAsync(token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask, typesTask, appSourcesTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new MailTemplateIndexViewModel
        {
            Rows = rows,
            Types = await typesTask,
            AppSources = await appSourcesTask,
            TotalCount = counts?.Total ?? 0,
            ActiveCount = counts?.Active ?? 0,
            PassiveCount = counts?.Passive ?? 0,
            DeletedCount = counts?.Deleted ?? 0,
            SearchName = name,
            ActiveFilter = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
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

}