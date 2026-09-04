using FurkanTural_Admin.Helpers;
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

    public async Task<IActionResult> Index(string? search = null, string? typeFilter = null, string? statusFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        var vm = await BuildViewModelAsync(token, search, typeFilter, statusFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? search = null, string? typeFilter = null, string? statusFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var vm = await BuildViewModelAsync(token, search, typeFilter, statusFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_ReportTable", vm);
    }

    private async Task<ReportIndexViewModel> BuildViewModelAsync(
        string token,
        string? search, string? typeFilter, string? statusFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(search, null, deletedFilter, dateFrom, dateTo, pageNumber, pageSize)
            .With("targetType", typeFilter)
            .With("status", statusFilter);
        var pendingRequest = new AdminListRequest { IsDeleted = false }.With("status", "Pending");
        var resolvedRequest = new AdminListRequest { IsDeleted = false }.WithAll("statuses", ResolvedStatuses);

        var countsTask = _reportApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pendingTask = _reportApiClient.GetAdminCountsAsync(pendingRequest, token, cancellationToken);
        var resolvedTask = _reportApiClient.GetAdminCountsAsync(resolvedRequest, token, cancellationToken);
        var pagedTask = _reportApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, pendingTask, resolvedTask, pagedTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new ReportIndexViewModel
        {
            Rows = rows,
            TotalCount = counts?.Total ?? 0,
            PendingCount = (await pendingTask)?.Total ?? 0,
            ResolvedCount = (await resolvedTask)?.Total ?? 0,
            DeletedCount = counts?.Deleted ?? 0,
            Search = search,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter,
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