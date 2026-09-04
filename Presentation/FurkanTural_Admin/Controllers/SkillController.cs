using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Skill;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class SkillController(ISkillApiClient skillApiClient) : Controller
{
    private readonly ISkillApiClient _skillApiClient = skillApiClient;

    public async Task<IActionResult> Index(
        string? name,
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

        var vm = await BuildViewModelAsync(token, name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? name,
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

        var vm = await BuildViewModelAsync(token, name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_SkillTable", vm);
    }

    private async Task<SkillIndexViewModel> BuildViewModelAsync(
        string token,
        string? name, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);

        var countsTask = _skillApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _skillApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new SkillIndexViewModel
        {
            Rows          = rows,
            TotalCount    = counts?.Total ?? 0,
            ActiveCount   = counts?.Active ?? 0,
            PassiveCount  = counts?.Passive ?? 0,
            DeletedCount  = counts?.Deleted ?? 0,
            SearchName    = name,
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

        var result = await _skillApiClient.DeleteAsync(id, token, cancellationToken);
        return result.ToActionResult("Silme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _skillApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return result.ToActionResult("Durum değiştirme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var result = await _skillApiClient.RestoreAsync(id, token, cancellationToken);
        return result.ToActionResult("Geri yükleme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] SkillFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (dto.Proficiency < 0 || dto.Proficiency > 100)
            return BadRequest(new { message = "Yetkinlik değeri 0-100 arasında olmalıdır." });

        var result = await _skillApiClient.CreateAsync(dto, token, cancellationToken);
        return result.ToActionResult("Kayıt oluşturulurken bir hata oluştu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] SkillFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (dto.Proficiency < 0 || dto.Proficiency > 100)
            return BadRequest(new { message = "Yetkinlik değeri 0-100 arasında olmalıdır." });

        var result = await _skillApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return result.ToActionResult("Kayıt güncellenirken bir hata oluştu.");
    }
}
