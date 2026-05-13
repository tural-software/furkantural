using FurkanTural_Admin.Models.Log;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class LogController(ILogApiClient logApiClient) : Controller
{
    private readonly ILogApiClient _logApiClient = logApiClient;

    public async Task<IActionResult> Index(
        string? levelFilter,
        string? searchProject,
        string? searchMessage,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        DateTime? from = DateTime.TryParse(dateFrom, out var df) ? df : null;
        DateTime? to   = DateTime.TryParse(dateTo, out var dt) ? dt : null;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var summaryTask = _logApiClient.GetAdminSummaryAsync(token, cancellationToken);
        var pagedTask   = _logApiClient.GetAdminPagedAsync(levelFilter, searchProject, searchMessage, from, to, safePageNumber, safePageSize, token, cancellationToken);

        await Task.WhenAll(summaryTask, pagedTask);

        var summary = await summaryTask;
        var (rows, totalFiltered) = await pagedTask;

        var vm = new LogIndexViewModel
        {
            Rows          = rows,
            TotalCount    = summary?.TotalCount ?? 0,
            LastActivityAt = summary?.LastActivityAt,
            LevelFilter   = levelFilter,
            SearchProject = searchProject,
            SearchMessage = searchMessage,
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
        string? levelFilter,
        string? searchProject,
        string? searchMessage,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        DateTime? from = DateTime.TryParse(dateFrom, out var df) ? df : null;
        DateTime? to   = DateTime.TryParse(dateTo, out var dt) ? dt : null;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var (rows, totalFiltered) = await _logApiClient.GetAdminPagedAsync(
            levelFilter, searchProject, searchMessage, from, to,
            safePageNumber, safePageSize, token, cancellationToken);

        var vm = new LogIndexViewModel
        {
            Rows          = rows,
            LevelFilter   = levelFilter,
            SearchProject = searchProject,
            SearchMessage = searchMessage,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };

        return PartialView("_LogTable", vm);
    }

    public IActionResult TableDetail()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        return View();
    }
}
