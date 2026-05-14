using FurkanTural_Admin.Models.Music;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class MusicController(IMusicApiClient musicApiClient) : Controller
{
    private readonly IMusicApiClient _musicApiClient = musicApiClient;

    private static MusicIndexViewModel BuildViewModel(
        IReadOnlyList<MusicAdminDto> all,
        string? searchName, string? searchArtist,
        string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo,
        int pageNumber, int pageSize,
        int? musicIdFilter = null)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchName))
            filtered = filtered.Where(m => m.Name != null && m.Name.Contains(searchName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(searchArtist))
            filtered = filtered.Where(m => m.Artist != null && m.Artist.Contains(searchArtist, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(m => m.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(m => !m.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(m => m.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(m => !m.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(m => m.CreatedAt >= from);

        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(m => m.CreatedAt < to.AddDays(1));

        if (musicIdFilter.HasValue)
            filtered = filtered.Where(m => m.Id == musicIdFilter.Value);

        var filteredList = filtered.ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize   = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new MusicIndexViewModel
        {
            Rows          = rows,
            TotalCount    = all.Count,
            ActiveCount   = all.Count(m => m.IsActive && !m.IsDeleted),
            PassiveCount  = all.Count(m => !m.IsActive && !m.IsDeleted),
            DeletedCount  = all.Count(m => m.IsDeleted),
            SearchName    = searchName,
            SearchArtist  = searchArtist,
            ActiveFilter  = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom      = dateFrom,
            DateTo        = dateTo,
            MusicIdFilter = musicIdFilter,
            PageNumber    = safePageNumber,
            PageSize      = safePageSize,
            TotalFiltered = totalFiltered
        };
    }

    public async Task<IActionResult> Index(
        string? searchName,
        string? searchArtist,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int? musicId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var all = await _musicApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, searchName, searchArtist, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, musicId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? searchName,
        string? searchArtist,
        string? activeFilter,
        string? deletedFilter,
        string? dateFrom,
        string? dateTo,
        int? musicId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var all = await _musicApiClient.GetAllForAdminAsync(token, cancellationToken);
        var vm = BuildViewModel(all, searchName, searchArtist, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, musicId);
        return PartialView("_MusicTable", vm);
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

        var ok = await _musicApiClient.DeleteAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Silme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _musicApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _musicApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] MusicFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _musicApiClient.CreateAsync(dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt oluşturulurken bir hata oluştu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] MusicFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var ok = await _musicApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Kayıt güncellenirken bir hata oluştu." });
    }
}
