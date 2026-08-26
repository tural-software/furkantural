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

    private static MusicImageIndexViewModel BuildViewModel(
        IReadOnlyList<MusicImageAdminDto> all,
        string? searchUrl,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? musicIdFilter,
        string? dateFrom,
        string? dateTo,
        int pageNumber,
        int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchUrl))
            filtered = filtered.Where(m => m.Url != null && m.Url.Contains(searchUrl, StringComparison.OrdinalIgnoreCase));

        if (isCoverFilter == "cover")
            filtered = filtered.Where(m => m.IsCover);
        else if (isCoverFilter == "notCover")
            filtered = filtered.Where(m => !m.IsCover);

        if (activeFilter == "active")
            filtered = filtered.Where(m => m.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(m => !m.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(m => m.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(m => !m.IsDeleted);

        if (musicIdFilter.HasValue)
            filtered = filtered.Where(m => m.MusicId == musicIdFilter.Value);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(m => m.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(m => m.CreatedAt < to.AddDays(1));

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new MusicImageIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(m => m.IsActive && !m.IsDeleted),
            PassiveCount  = all.Count(m => !m.IsActive && !m.IsDeleted),
            DeletedCount  = all.Count(m => m.IsDeleted),
            SearchUrl     = searchUrl,
            IsCoverFilter = isCoverFilter,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            MusicIdFilter = musicIdFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };
    }

    public async Task<IActionResult> Index(
        string? url,
        string? isCoverFilter,
        string? activeFilter,
        string? deletedFilter,
        int? musicId,
        string? dateFrom,
        string? dateTo,
        int pageNumber = 1,
        int pageSize   = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _musicImageApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm  = BuildViewModel(all, url, isCoverFilter, activeFilter, deletedFilter, musicId, dateFrom, dateTo, pageNumber, pageSize);

        ViewData["ApiBaseUrl"] = _apiBaseUrl;
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
        int pageSize   = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _musicImageApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm  = BuildViewModel(all, url, isCoverFilter, activeFilter, deletedFilter, musicId, dateFrom, dateTo, pageNumber, pageSize);

        return PartialView("_MusicImageTable", vm);
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