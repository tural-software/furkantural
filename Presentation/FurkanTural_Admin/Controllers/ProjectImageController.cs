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

    private static ProjectImageIndexViewModel BuildViewModel(
        IReadOnlyList<ProjectImageAdminDto> all,
        string? searchUrl,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? projectIdFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber,
        int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchUrl))
            filtered = filtered.Where(p => p.Url != null && p.Url.Contains(searchUrl, StringComparison.OrdinalIgnoreCase));

        if (isCoverFilter == "cover")
            filtered = filtered.Where(p => p.IsCover);
        else if (isCoverFilter == "notCover")
            filtered = filtered.Where(p => !p.IsCover);

        if (activeFilter == "active")
            filtered = filtered.Where(p => p.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(p => !p.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(p => p.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(p => !p.IsDeleted);

        if (projectIdFilter.HasValue)
            filtered = filtered.Where(p => p.ProjectId == projectIdFilter.Value);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(p => p.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(p => p.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new ProjectImageIndexViewModel
        {
            Rows            = rows,
            TotalCount      = all.Count,
            ActiveCount     = all.Count(p => p.IsActive && !p.IsDeleted),
            PassiveCount    = all.Count(p => !p.IsActive && !p.IsDeleted),
            DeletedCount    = all.Count(p => p.IsDeleted),
            SearchUrl       = searchUrl,
            IsCoverFilter   = isCoverFilter,
            ActiveFilter    = activeFilter,
            DeletedFilter   = deletedFilter,
            ProjectIdFilter = projectIdFilter,
            DateFrom        = dateFrom,
            DateTo          = dateTo,
            PageNumber      = safePageNumber,
            PageSize        = safePageSize,
            TotalFiltered   = totalFiltered
        };
    }

    public async Task<IActionResult> Index(
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? projectId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize   = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _projectImageApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm  = BuildViewModel(all, url, isCoverFilter, activeFilter, deletedFilter, projectId, dateFrom, dateTo, pageNumber, pageSize);

        ViewData["ApiBaseUrl"] = _apiBaseUrl;
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
        int pageSize   = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _projectImageApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm  = BuildViewModel(all, url, isCoverFilter, activeFilter, deletedFilter, projectId, dateFrom, dateTo, pageNumber, pageSize);

        return PartialView("_ProjectImageTable", vm);
    }

    public IActionResult TableDetail()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ProjectOptions(CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var projects = await _projectApiClient.GetAllForAdminAsync(token, cancellationToken);
        var options = projects
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Title)
            .Select(p => new { value = p.Id, label = p.Title ?? $"Proje #{p.Id}" })
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
