using FurkanTural_Admin.Models.Contact;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class ContactController(IContactApiClient contactApiClient) : Controller
{
    private readonly IContactApiClient _contactApiClient = contactApiClient;

    public async Task<IActionResult> Index(
        string? name,
        string? activeFilter,
        string? deletedFilter,
        string? readFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _contactApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, name, activeFilter, deletedFilter, readFilter, dateFrom, dateTo, pageNumber, pageSize);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? name,
        string? activeFilter,
        string? deletedFilter,
        string? readFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _contactApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, name, activeFilter, deletedFilter, readFilter, dateFrom, dateTo, pageNumber, pageSize);
        return PartialView("_ContactTable", vm);
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
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _contactApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _contactApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _contactApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _contactApiClient.MarkAsReadAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Okundu işareti başarısız oldu." });
    }

    private static ContactIndexViewModel BuildViewModel(
        IReadOnlyList<ContactAdminDto> all,
        string? name, string? activeFilter, string? deletedFilter, string? readFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(name))
            filtered = filtered.Where(s => (s.Name != null && s.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                                        || (s.Email != null && s.Email.Contains(name, StringComparison.OrdinalIgnoreCase)));

        if (activeFilter == "active")
            filtered = filtered.Where(s => s.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(s => !s.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(s => s.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(s => !s.IsDeleted);

        if (readFilter == "read")
            filtered = filtered.Where(s => s.IsRead);
        else if (readFilter == "unread")
            filtered = filtered.Where(s => !s.IsRead);

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

        return new ContactIndexViewModel
        {
            Rows = rows,
            TotalCount = all.Count,
            ActiveCount = all.Count(s => s.IsActive && !s.IsDeleted),
            PassiveCount = all.Count(s => !s.IsActive && !s.IsDeleted),
            DeletedCount = all.Count(s => s.IsDeleted),
            UnreadCount = all.Count(s => !s.IsRead && !s.IsDeleted),
            SearchName = name,
            ActiveFilter = activeFilter,
            DeletedFilter = deletedFilter,
            ReadFilter = readFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalFiltered = totalFiltered
        };
    }
}