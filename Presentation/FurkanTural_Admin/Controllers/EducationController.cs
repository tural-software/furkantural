using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Education;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class EducationController(IEducationApiClient educationApiClient) : Controller
{
    private readonly IEducationApiClient _educationApiClient = educationApiClient;

    public async Task<IActionResult> Index(
        string? institution,
        string? degree,
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

        var all = await _educationApiClient.GetAllForAdminAsync(token, cancellationToken);

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(institution))
            filtered = filtered.Where(e => e.Institution != null && e.Institution.Contains(institution, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(degree))
            filtered = filtered.Where(e => e.Degree != null && e.Degree.Contains(degree, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(e => e.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(e => !e.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(e => e.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(e => !e.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(e => e.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(e => e.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new EducationIndexViewModel
        {
            Rows              = rows,
            TotalCount        = all.Count,
            ActiveCount       = all.Count(e => e.IsActive && !e.IsDeleted),
            PassiveCount      = all.Count(e => !e.IsActive && !e.IsDeleted),
            DeletedCount      = all.Count(e => e.IsDeleted),
            SearchInstitution = institution,
            SearchDegree      = degree,
            ActiveFilter      = activeFilter,
            DeletedFilter     = deletedFilter,
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            PageNumber        = safePageNumber,
            PageSize          = safePageSize,
            TotalFiltered     = totalFiltered
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? institution,
        string? degree,
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

        var all = await _educationApiClient.GetAllForAdminAsync(token, cancellationToken);

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(institution))
            filtered = filtered.Where(e => e.Institution != null && e.Institution.Contains(institution, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(degree))
            filtered = filtered.Where(e => e.Degree != null && e.Degree.Contains(degree, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(e => e.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(e => !e.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(e => e.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(e => !e.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(e => e.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(e => e.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new EducationIndexViewModel
        {
            Rows              = rows,
            TotalCount        = all.Count,
            ActiveCount       = all.Count(e => e.IsActive && !e.IsDeleted),
            PassiveCount      = all.Count(e => !e.IsActive && !e.IsDeleted),
            DeletedCount      = all.Count(e => e.IsDeleted),
            SearchInstitution = institution,
            SearchDegree      = degree,
            ActiveFilter      = activeFilter,
            DeletedFilter     = deletedFilter,
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            PageNumber        = safePageNumber,
            PageSize          = safePageSize,
            TotalFiltered     = totalFiltered
        };

        return PartialView("_EducationTable", vm);
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

        var ok = await _educationApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _educationApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _educationApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] EducationFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _educationApiClient.CreateAsync(dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] EducationFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _educationApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt güncellenirken bir hata oluştu." });
    }
}