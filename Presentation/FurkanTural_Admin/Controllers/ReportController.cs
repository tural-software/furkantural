using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Report;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FurkanTural_Admin.Controllers;

public class ReportController(IReportApiClient reportApiClient, IOptions<ApiOptions> apiOptions) : Controller
{
    private readonly IReportApiClient _reportApiClient = reportApiClient;
    private readonly ApiOptions _apiOptions = apiOptions.Value;

    private static readonly string[] ResolvedStatuses = ["Reviewed", "Dismissed", "ActionTaken"];

    private static ReportIndexViewModel BuildViewModel(
        IReadOnlyList<ReportAdminDto> all,
        string? search, string? typeFilter, string? statusFilter, string? deletedFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(r =>
                (r.ReporterName != null && r.ReporterName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (r.ReportedUserName != null && r.ReportedUserName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (r.Reason != null && r.Reason.Contains(search, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(typeFilter))
            filtered = filtered.Where(r => string.Equals(r.TargetType, typeFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(statusFilter))
            filtered = filtered.Where(r => string.Equals(r.Status, statusFilter, StringComparison.OrdinalIgnoreCase));

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

        return new ReportIndexViewModel
        {
            Rows = rows,
            TotalCount = all.Count,
            PendingCount = all.Count(r => string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase) && !r.IsDeleted),
            ResolvedCount = all.Count(r => ResolvedStatuses.Contains(r.Status) && !r.IsDeleted),
            DeletedCount = all.Count(r => r.IsDeleted),
            Search = search,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter,
            DeletedFilter = deletedFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalFiltered = totalFiltered
        };
    }

    public async Task<IActionResult> Index(string? search = null, string? typeFilter = null, string? statusFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var all = await _reportApiClient.GetAllForAdminAsync(token, cancellationToken);
        return View(BuildViewModel(all, search, typeFilter, statusFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    public IActionResult TableDetail()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? search = null, string? typeFilter = null, string? statusFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var all = await _reportApiClient.GetAllForAdminAsync(token, cancellationToken);
        return PartialView("_ReportTable", BuildViewModel(all, search, typeFilter, statusFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? adminNote = null, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _reportApiClient.UpdateStatusAsync(id, status, adminNote, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum güncelleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _reportApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _reportApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }
}
