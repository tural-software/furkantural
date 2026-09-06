using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.UserFriend;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class UserFriendController(IUserFriendApiClient userFriendApiClient) : Controller
{
    private readonly IUserFriendApiClient _userFriendApiClient = userFriendApiClient;

    public async Task<IActionResult> Index(string? statusFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        return View(await BuildViewModelAsync(token, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? statusFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        return PartialView("_UserFriendTable", await BuildViewModelAsync(token, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken));
    }

    private async Task<UserFriendIndexViewModel> BuildViewModelAsync(
        string token,
        string? statusFilter, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(null, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize)
            .With("statusCode", statusFilter);

        var countsTask = _userFriendApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _userFriendApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new UserFriendIndexViewModel
        {
            Rows = rows,
            TotalCount = counts?.Total ?? 0,
            ActiveCount = counts?.Active ?? 0,
            PassiveCount = counts?.Passive ?? 0,
            DeletedCount = counts?.Deleted ?? 0,
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

        var result = await _userFriendApiClient.BulkAsync(action.Trim().ToLowerInvariant(), wanted, token, cancellationToken);
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