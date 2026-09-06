using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.ProjectImage;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class ProjectImageController(
    IProjectImageApiClient projectImageApiClient,
    IProjectApiClient projectApiClient,
    IConfiguration configuration) : Controller
{
    private readonly IProjectImageApiClient _projectImageApiClient = projectImageApiClient;
    private readonly IProjectApiClient      _projectApiClient      = projectApiClient;
    private readonly string                 _apiBaseUrl            = configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty;

    public async Task<IActionResult> Index(
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? projectId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var vm = await BuildViewModelAsync(token, url, isCoverFilter, activeFilter, deletedFilter, projectId, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? projectId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var vm = await BuildViewModelAsync(token, url, isCoverFilter, activeFilter, deletedFilter, projectId, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_ProjectImageTable", vm);
    }

    private async Task<ProjectImageIndexViewModel> BuildViewModelAsync(
        string token,
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? projectId,
        string? dateFrom,
        string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(url, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize).With("isCover", isCoverFilter switch { "cover" => true, "notCover" => false, _ => (bool?)null }).With("projectId", projectId);

        var countsTask = _projectImageApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _projectImageApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new ProjectImageIndexViewModel
        {
            Rows          = rows,
            TotalCount    = counts?.Total ?? 0,
            ActiveCount   = counts?.Active ?? 0,
            PassiveCount  = counts?.Passive ?? 0,
            DeletedCount  = counts?.Deleted ?? 0,
            SearchUrl     = url,
            IsCoverFilter = isCoverFilter,
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

    [HttpGet]
    public async Task<IActionResult> ProjectOptions(string? search, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var options = await _projectApiClient.GetAdminOptionsAsync(search, null, token, cancellationToken);
        return Json(options.Select(o => new { value = o.Id, label = o.Label ?? "" }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(string? action, int[]? ids, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var wanted = (ids ?? []).Where(i => i > 0).Distinct().ToList();
        if (string.IsNullOrWhiteSpace(action) || wanted.Count == 0)
            return BadRequest(new { message = "İşlem türü ve en az bir kayıt gerekir." });

        var result = await _projectImageApiClient.BulkAsync(action.Trim().ToLowerInvariant(), wanted, token, cancellationToken);
        return result is null
            ? StatusCode(500, new { message = "Toplu işlem başarısız oldu." })
            : Json(new { requested = result.Requested, affected = result.Affected, skipped = result.Skipped });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _projectImageApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _projectImageApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _projectImageApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        IFormFile? imageFile,
        string? altText,
        bool isCover    = false,
        int projectId   = 0,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (imageFile == null || imageFile.Length == 0)
            return BadRequest(new { message = "Görsel dosyası zorunludur." });

        if (projectId <= 0)
            return BadRequest(new { message = "Geçerli bir Proje ID giriniz." });

        if (altText != null && altText.Length > 500)
            return BadRequest(new { message = "Açıklama metni en fazla 500 karakter olabilir." });

        var newId = await _projectImageApiClient.CreateAsync(imageFile, altText, isCover, projectId, token, cancellationToken);
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
        bool isCover         = false,
        int projectId        = 0,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (projectId <= 0)
            return BadRequest(new { message = "Geçerli bir Proje ID giriniz." });

        if (altText != null && altText.Length > 500)
            return BadRequest(new { message = "Açıklama metni en fazla 500 karakter olabilir." });

        var ok = await _projectImageApiClient.UpdateAsync(id, imageFile, altText, isCover, projectId, token, cancellationToken);
        if (!ok)
            return StatusCode(500, new { message = "Güncelleme işlemi başarısız oldu." });

        return Ok();
    }
}