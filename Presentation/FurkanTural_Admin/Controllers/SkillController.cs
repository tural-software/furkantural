using FurkanTural_Admin.Models.Skill;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class SkillController(ISkillApiClient skillApiClient) : Controller
{
    private readonly ISkillApiClient _skillApiClient = skillApiClient;

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

        var all = await _skillApiClient.GetAllForAdminAsync(token, cancellationToken);

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

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new SkillIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(s => s.IsActive && !s.IsDeleted),
            PassiveCount  = all.Count(s => !s.IsActive && !s.IsDeleted),
            DeletedCount  = all.Count(s => s.IsDeleted),
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

        var all = await _skillApiClient.GetAllForAdminAsync(token, cancellationToken);

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

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new SkillIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(s => s.IsActive && !s.IsDeleted),
            PassiveCount  = all.Count(s => !s.IsActive && !s.IsDeleted),
            DeletedCount  = all.Count(s => s.IsDeleted),
            SearchName    = name,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };

        return PartialView("_SkillTable", vm);
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

        var result = await _skillApiClient.DeleteAsync(id, token, cancellationToken);
        return result.ToActionResult("Silme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _skillApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return result.ToActionResult("Durum değiştirme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _skillApiClient.RestoreAsync(id, token, cancellationToken);
        return result.ToActionResult("Geri yükleme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] SkillFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (dto.Proficiency < 0 || dto.Proficiency > 100)
            return BadRequest(new { message = "Yetkinlik değeri 0-100 arasında olmalıdır." });

        var result = await _skillApiClient.CreateAsync(dto, token, cancellationToken);
        return result.ToActionResult("Kayıt oluşturulurken bir hata oluştu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] SkillFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (dto.Proficiency < 0 || dto.Proficiency > 100)
            return BadRequest(new { message = "Yetkinlik değeri 0-100 arasında olmalıdır." });

        var result = await _skillApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return result.ToActionResult("Kayıt güncellenirken bir hata oluştu.");
    }
}
