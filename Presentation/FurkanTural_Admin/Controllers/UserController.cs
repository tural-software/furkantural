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

    private async Task<(IReadOnlyList<UserAdminDto> users, IReadOnlyList<RoleAdminDto> roles)>
        FetchUsersAndRoles(string token, CancellationToken cancellationToken)
    {
        var usersTask = _userApiClient.GetAllForAdminAsync(token, cancellationToken);
        var rolesTask = _roleApiClient.GetAllForAdminAsync(token, cancellationToken);

        await Task.WhenAll(usersTask, rolesTask);

        var users = usersTask.Result;
        var roles = rolesTask.Result;

        var roleDict = roles.ToDictionary(r => r.Id, r => r.Name);
        foreach (var u in users)
            u.RoleName = roleDict.GetValueOrDefault(u.RoleId);

        return (users, roles);
    }

    private static UserIndexViewModel BuildViewModel(
        IReadOnlyList<UserAdminDto> all,
        IReadOnlyList<RoleAdminDto> roles,
        string? searchUsername, int? roleFilter,
        string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo,
        int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchUsername))
            filtered = filtered.Where(u => u.Username != null && u.Username.Contains(searchUsername, StringComparison.OrdinalIgnoreCase));

        if (roleFilter.HasValue && roleFilter.Value > 0)
            filtered = filtered.Where(u => u.RoleId == roleFilter.Value);

        if (activeFilter == "active")
            filtered = filtered.Where(u => u.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(u => !u.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(u => u.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(u => !u.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(u => u.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(u => u.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new UserIndexViewModel
        {
            Rows          = rows,
            RoleOptions   = roles,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(u => u.IsActive && !u.IsDeleted),
            PassiveCount  = all.Count(u => !u.IsActive && !u.IsDeleted),
            DeletedCount  = all.Count(u => u.IsDeleted),
            SearchUsername = searchUsername,
            RoleFilter    = roleFilter,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };
    }

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
        var (all, roles) = await FetchUsersAndRoles(token, cancellationToken);
        var vm = BuildViewModel(all, roles, searchUsername, roleFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);
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
        var (all, roles) = await FetchUsersAndRoles(token, cancellationToken);
        var vm = BuildViewModel(all, roles, searchUsername, roleFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);
        return PartialView("_UserTable", vm);
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
