using FurkanTural_Admin.Models.UserFriend;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class UserFriendController(IUserFriendApiClient userFriendApiClient) : Controller
{
    private readonly IUserFriendApiClient _userFriendApiClient = userFriendApiClient;

    private static UserFriendIndexViewModel BuildViewModel(
        IReadOnlyList<UserFriendAdminDto> all,
        string? statusFilter, string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
            filtered = filtered.Where(r => r.StatusCode != null && r.StatusCode.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));

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

        var filteredList = filtered.OrderByDescending(r => r.CreatedAt).ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList.Skip((safePageNumber - 1) * safePageSize).Take(safePageSize).ToList();

        return new UserFriendIndexViewModel
        {
            Rows = rows,
            TotalCount = all.Count,
            ActiveCount = all.Count(r => r.IsActive && !r.IsDeleted),
            PassiveCount = all.Count(r => !r.IsActive && !r.IsDeleted),
            DeletedCount = all.Count(r => r.IsDeleted),
            StatusFilter = statusFilter,
            ActiveFilter = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalFiltered = totalFiltered
        };
    }

    public async Task<IActionResult> Index(string? statusFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        var all = await _userFriendApiClient.GetAllForAdminAsync(token, cancellationToken);
        return View(BuildViewModel(all, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? statusFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var all = await _userFriendApiClient.GetAllForAdminAsync(token, cancellationToken);
        return PartialView("_UserFriendTable", BuildViewModel(all, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _userFriendApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _userFriendApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }
}
