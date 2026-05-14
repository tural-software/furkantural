using FurkanTural_Admin.Models.Project;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class ProjectController(IProjectApiClient projectApiClient) : Controller
{
    private readonly IProjectApiClient _projectApiClient = projectApiClient;

    private static ProjectIndexViewModel BuildViewModel(
        IReadOnlyList<ProjectAdminDto> all,
        string? searchTitle,
        string? completedFilter,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber,
        int pageSize,
        int? projectIdFilter = null)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTitle))
            filtered = filtered.Where(p => p.Title != null && p.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase));

        if (completedFilter == "completed")
            filtered = filtered.Where(p => p.IsCompleted);
        else if (completedFilter == "notCompleted")
            filtered = filtered.Where(p => !p.IsCompleted);

        if (activeFilter == "active")
            filtered = filtered.Where(p => p.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(p => !p.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(p => p.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(p => !p.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(p => p.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(p => p.CreatedAt < to.AddDays(1));

        if (projectIdFilter.HasValue)
            filtered = filtered.Where(p => p.Id == projectIdFilter.Value);

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new ProjectIndexViewModel
        {
            Rows            = rows,
            TotalCount      = all.Count,
            ActiveCount     = all.Count(p => p.IsActive && !p.IsDeleted),
            PassiveCount    = all.Count(p => !p.IsActive && !p.IsDeleted),
            DeletedCount    = all.Count(p => p.IsDeleted),
            SearchTitle     = searchTitle,
            CompletedFilter = completedFilter,
            ActiveFilter    = activeFilter,
            DeletedFilter   = deletedFilter,
            DateFrom        = dateFrom,
            DateTo          = dateTo,
            ProjectIdFilter = projectIdFilter,
            PageNumber      = safePageNumber,
            PageSize        = safePageSize,
            TotalFiltered   = totalFiltered
        };
    }

    public async Task<IActionResult> Index(
        string? searchTitle,
        string? completedFilter,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int? projectId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _projectApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, searchTitle, completedFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, projectId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? searchTitle,
        string? completedFilter,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int? projectId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _projectApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, searchTitle, completedFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, projectId);
        return PartialView("_ProjectTable", vm);
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

    public IActionResult TableDetail()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _projectApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _projectApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _projectApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] ProjectFormDto dto,
        bool isActive = true,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _projectApiClient.CreateAsync(dto, token, cancellationToken);
        if (!ok)
            return StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });

        if (!isActive)
        {
            var all = await _projectApiClient.GetAllForAdminAsync(token, cancellationToken);
            var created = all.OrderByDescending(p => p.CreatedAt).FirstOrDefault(p => p.Title == dto.Title);
            if (created != null)
                await _projectApiClient.ToggleActiveAsync(created.Id, token, cancellationToken);
        }

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] ProjectFormDto dto,
        bool isActive = true,
        bool currentIsActive = true,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _projectApiClient.UpdateAsync(id, dto, token, cancellationToken);
        if (!ok)
            return StatusCode(500, new { message = "Kayıt güncellenirken bir hata oluştu." });

        if (isActive != currentIsActive)
            await _projectApiClient.ToggleActiveAsync(id, token, cancellationToken);

        return Ok();
    }
}
