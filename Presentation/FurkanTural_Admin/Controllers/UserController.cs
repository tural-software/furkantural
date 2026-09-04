using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Role;
using FurkanTural_Admin.Models.User;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FurkanTural_Admin.Controllers;

public class UserController(IUserApiClient userApiClient, IRoleApiClient roleApiClient, IOptions<ApiOptions> apiOptions) : Controller
{
    private readonly IUserApiClient _userApiClient = userApiClient;
    private readonly IRoleApiClient _roleApiClient = roleApiClient;
    private readonly ApiOptions _apiOptions = apiOptions.Value;

    public async Task<IActionResult> Index(
        string? searchUsername,
        int? roleFilter,
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

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var vm = await BuildViewModelAsync(token, searchUsername, roleFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? searchUsername,
        int? roleFilter,
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

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var vm = await BuildViewModelAsync(token, searchUsername, roleFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_UserTable", vm);
    }

    private async Task<UserIndexViewModel> BuildViewModelAsync(
        string token,
        string? searchUsername, int? roleFilter, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(searchUsername, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize)
            .With("roleId", roleFilter);

        var countsTask = _userApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _userApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        var rolesTask = _roleApiClient.GetAdminOptionsAsync(null, null, token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask, rolesTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new UserIndexViewModel
        {
            Rows          = rows,
            RoleOptions   = await rolesTask,
            TotalCount    = counts?.Total ?? 0,
            ActiveCount   = counts?.Active ?? 0,
            PassiveCount  = counts?.Passive ?? 0,
            DeletedCount  = counts?.Deleted ?? 0,
            SearchUsername = searchUsername,
            RoleFilter    = roleFilter,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = request.PageNumber,
            PageSize      = request.PageSize,
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
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _userApiClient.DeleteAsync(id, token, cancellationToken);
        return result.ToActionResult("Silme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _userApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return result.ToActionResult("Durum değiştirme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _userApiClient.RestoreAsync(id, token, cancellationToken);
        return result.ToActionResult("Geri yükleme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] UserFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _userApiClient.CreateAsync(dto, token, cancellationToken);
        return result.ToActionResult("Kayıt oluşturulurken bir hata oluştu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] UserFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _userApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return result.ToActionResult("Kayıt güncellenirken bir hata oluştu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(int id, IFormFile? avatarFile, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (avatarFile is null || avatarFile.Length == 0)
            return BadRequest(new { message = "Avatar dosyası zorunludur." });

        var result = await _userApiClient.UploadAvatarAsync(id, avatarFile, token, cancellationToken);
        return result.ToActionResult("Avatar yüklenirken bir hata oluştu.");
    }
}