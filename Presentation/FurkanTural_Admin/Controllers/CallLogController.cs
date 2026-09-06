using FurkanTural_Admin.Helpers;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(CallPolicyFormDto form, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _callPolicyApiClient.UpdateAsync(form, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Ayar kaydedilemedi." });
    }

    public async Task<IActionResult> Index(string? search = null, string? typeFilter = null, string? statusFilter = null,
        string? activeFilter = null, string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var vm = await BuildViewModelAsync(token, search, typeFilter, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        vm.Policy = await _callPolicyApiClient.GetAsync(token, cancellationToken) ?? new CallPolicyFormDto();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? search = null, string? typeFilter = null, string? statusFilter = null,
        string? activeFilter = null, string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var vm = await BuildViewModelAsync(token, search, typeFilter, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_CallLogTable", vm);
    }

    private async Task<CallLogIndexViewModel> BuildViewModelAsync(
        string token,
        string? search, string? typeFilter, string? statusFilter, string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(search, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize)
            .With("callType", typeFilter)
            .With("status", statusFilter);

        var countsTask = _callLogApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _callLogApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new CallLogIndexViewModel
        {
            Rows = rows,
            TotalCount = counts?.Total ?? 0,
            ActiveCount = counts?.Active ?? 0,
            PassiveCount = counts?.Passive ?? 0,
            DeletedCount = counts?.Deleted ?? 0,
            Search = search,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter,
            ActiveFilter = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
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

        var result = await _callLogApiClient.BulkAsync(action.Trim().ToLowerInvariant(), wanted, token, cancellationToken);
        return result is null
            ? StatusCode(500, new { message = "Toplu işlem başarısız oldu." })
            : Json(new { requested = result.Requested, affected = result.Affected, skipped = result.Skipped });
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