using FurkanTural_Admin.Models.Call;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FurkanTural_Admin.Controllers;

public class CallLogController(ICallLogApiClient callLogApiClient, ICallPolicyApiClient callPolicyApiClient, IOptions<ApiOptions> apiOptions) : Controller
{
    private readonly ICallLogApiClient _callLogApiClient = callLogApiClient;
    private readonly ICallPolicyApiClient _callPolicyApiClient = callPolicyApiClient;
    private readonly ApiOptions _apiOptions = apiOptions.Value;

    private static CallLogIndexViewModel BuildViewModel(
        IReadOnlyList<CallLogAdminDto> all,
        string? search, string? typeFilter, string? statusFilter, string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(r =>
                (r.CallerName != null && r.CallerName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (r.CalleeName != null && r.CalleeName.Contains(search, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(typeFilter))
            filtered = filtered.Where(r => string.Equals(r.CallType, typeFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(statusFilter))
            filtered = filtered.Where(r => string.Equals(r.Status, statusFilter, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(r => r.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(r => !r.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(r => r.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(r => !r.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(r => r.StartedAt >= from);
        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(r => r.StartedAt < to.AddDays(1));

        var filteredList = filtered.OrderByDescending(r => r.StartedAt).ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList.Skip((safePageNumber - 1) * safePageSize).Take(safePageSize).ToList();

        return new CallLogIndexViewModel
        {
            Rows = rows,
            TotalCount = all.Count,
            ActiveCount = all.Count(r => r.IsActive && !r.IsDeleted),
            PassiveCount = all.Count(r => !r.IsActive && !r.IsDeleted),
            DeletedCount = all.Count(r => r.IsDeleted),
            Search = search,
            TypeFilter = typeFilter,
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

    public async Task<IActionResult> Index(string? search = null, string? typeFilter = null, string? statusFilter = null,
        string? activeFilter = null, string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var all = await _callLogApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, search, typeFilter, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);
        vm.Policy = await _callPolicyApiClient.GetAsync(token, cancellationToken) ?? new CallPolicyFormDto();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(CallPolicyFormDto form, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _callPolicyApiClient.UpdateAsync(form, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Ayar kaydedilemedi." });
    }

    public IActionResult TableDetail()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? search = null, string? typeFilter = null, string? statusFilter = null,
        string? activeFilter = null, string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var all = await _callLogApiClient.GetAllForAdminAsync(token, cancellationToken);
        return PartialView("_CallLogTable", BuildViewModel(all, search, typeFilter, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _callLogApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _callLogApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }
}
