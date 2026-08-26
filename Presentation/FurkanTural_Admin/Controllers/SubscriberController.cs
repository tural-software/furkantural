using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Subscriber;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class SubscriberController(ISubscriberApiClient subscriberApiClient) : Controller
{
    private readonly ISubscriberApiClient _subscriberApiClient = subscriberApiClient;

    public async Task<IActionResult> Index(
        string? email,
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

        var all = await _subscriberApiClient.GetAllForAdminAsync(token, cancellationToken);

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(email))
            filtered = filtered.Where(s => s.Email != null && s.Email.Contains(email, StringComparison.OrdinalIgnoreCase));

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

        var safePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var vm = new SubscriberIndexViewModel
        {
            Rows = rows,
            TotalCount = all.Count,
            ActiveCount = all.Count(s => s.IsActive && !s.IsDeleted),
            PassiveCount = all.Count(s => !s.IsActive && !s.IsDeleted),
            DeletedCount = all.Count(s => s.IsDeleted),
            SearchEmail = email,
            ActiveFilter = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalFiltered = totalFiltered
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? email,
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

        var all = await _subscriberApiClient.GetAllForAdminAsync(token, cancellationToken);

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(email))
            filtered = filtered.Where(s => s.Email != null && s.Email.Contains(email, StringComparison.OrdinalIgnoreCase));

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

        var vm = new SubscriberIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(s => s.IsActive && !s.IsDeleted),
            PassiveCount  = all.Count(s => !s.IsActive && !s.IsDeleted),
            DeletedCount  = all.Count(s => s.IsDeleted),
            SearchEmail   = email,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };

        return PartialView("_SubscriberTable", vm);
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

        await _subscriberApiClient.DeleteAsync(id, token, cancellationToken);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        await _subscriberApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        await _subscriberApiClient.RestoreAsync(id, token, cancellationToken);
        return Ok();
    }
}