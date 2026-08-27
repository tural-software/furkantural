using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Role;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class RoleController(IRoleApiClient roleApiClient) : Controller
{
    private readonly IRoleApiClient _roleApiClient = roleApiClient;

    public async Task<IActionResult> Index(
        string? name,
        string? activeFilter = null,
        string? deletedFilter = null,
        string? dateFrom = null,
        string? dateTo = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _roleApiClient.GetAllForAdminAsync(token, cancellationToken);

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(name))
            filtered = filtered.Where(r => r.Name != null && r.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(r => r.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(r => !r.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(r => r.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(r => !r.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(r => r.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(r => r.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new RoleIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(r => r.IsActive && !r.IsDeleted),
            PassiveCount  = all.Count(r => !r.IsActive && !r.IsDeleted),
            DeletedCount  = all.Count(r => r.IsDeleted),
            SearchName    = name,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? name,
        string? activeFilter = null,
        string? deletedFilter = null,
        string? dateFrom = null,
        string? dateTo = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _roleApiClient.GetAllForAdminAsync(token, cancellationToken);

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(name))
            filtered = filtered.Where(r => r.Name != null && r.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(r => r.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(r => !r.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(r => r.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(r => !r.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(r => r.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(r => r.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new RoleIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(r => r.IsActive && !r.IsDeleted),
            PassiveCount  = all.Count(r => !r.IsActive && !r.IsDeleted),
            DeletedCount  = all.Count(r => r.IsDeleted),
            SearchName    = name,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };

        return PartialView("_RoleTable", vm);
    }

    [HttpGet]
    public async Task<IActionResult> RoleOptions(CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _roleApiClient.GetAllForAdminAsync(token, cancellationToken);
        var options = all
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new { value = r.Id, label = r.Name ?? $"Rol #{r.Id}" });

        return Json(options);
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

        var ok = await _roleApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _roleApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _roleApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] RoleFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _roleApiClient.CreateAsync(dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] RoleFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _roleApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt güncellenirken bir hata oluştu." });
    }
}