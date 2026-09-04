using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.MusicImage;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class MusicImageController(
    IMusicImageApiClient musicImageApiClient,
    IMusicApiClient musicApiClient,
    IConfiguration configuration) : Controller
{
    private readonly IMusicImageApiClient _musicImageApiClient = musicImageApiClient;
    private readonly IMusicApiClient      _musicApiClient      = musicApiClient;
    private readonly string               _apiBaseUrl          = configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty;

    public async Task<IActionResult> Index(
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? musicId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var vm = await BuildViewModelAsync(token, url, isCoverFilter, activeFilter, deletedFilter, musicId, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? musicId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var vm = await BuildViewModelAsync(token, url, isCoverFilter, activeFilter, deletedFilter, musicId, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_MusicImageTable", vm);
    }

    private async Task<MusicImageIndexViewModel> BuildViewModelAsync(
        string token,
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? musicId,
        string? dateFrom,
        string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(url, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize).With("isCover", isCoverFilter switch { "cover" => true, "notCover" => false, _ => (bool?)null }).With("musicId", musicId);

        var countsTask = _musicImageApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _musicImageApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new MusicImageIndexViewModel
        {
            Rows          = rows,
            TotalCount    = counts?.Total ?? 0,
            ActiveCount   = counts?.Active ?? 0,
            PassiveCount  = counts?.Passive ?? 0,
            DeletedCount  = counts?.Deleted ?? 0,
            SearchUrl     = url,
            IsCoverFilter = isCoverFilter,
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

    [HttpGet]
    public async Task<IActionResult> MusicOptions(CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var musics = await _musicApiClient.GetAllForAdminAsync(token, cancellationToken);
        var options = musics
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Name)
            .Select(m => new { value = m.Id, label = m.Name ?? $"Müzik #{m.Id}" })
            .ToList();

        return Json(options);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _musicImageApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _musicImageApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _musicImageApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        IFormFile? imageFile,
        string? altText,
        bool isCover   = false,
        int musicId    = 0,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (imageFile == null || imageFile.Length == 0)
            return BadRequest(new { message = "Görsel dosyası zorunludur." });

        if (musicId <= 0)
            return BadRequest(new { message = "Geçerli bir Müzik ID giriniz." });

        if (altText != null && altText.Length > 500)
            return BadRequest(new { message = "Açıklama metni en fazla 500 karakter olabilir." });

        var newId = await _musicImageApiClient.CreateAsync(imageFile, altText, isCover, musicId, token, cancellationToken);
        if (!newId.HasValue)
            return StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        IFormFile? imageFile,
        string? altText,
        bool isCover        = false,
        int musicId         = 0,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        if (musicId <= 0)
            return BadRequest(new { message = "Geçerli bir Müzik ID giriniz." });

        if (altText != null && altText.Length > 500)
            return BadRequest(new { message = "Açıklama metni en fazla 500 karakter olabilir." });

        var ok = await _musicImageApiClient.UpdateAsync(id, imageFile, altText, isCover, musicId, token, cancellationToken);
        if (!ok)
            return StatusCode(500, new { message = "Güncelleme işlemi başarısız oldu." });

        return Ok();
    }
}