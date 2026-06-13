using FurkanTural_Admin.Models.ChatMessage;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FurkanTural_Admin.Controllers;

public class ChatMessageController(IChatMessageApiClient chatMessageApiClient, IOptions<ApiOptions> apiOptions) : Controller
{
    private readonly IChatMessageApiClient _chatMessageApiClient = chatMessageApiClient;
    private readonly ApiOptions _apiOptions = apiOptions.Value;

    private static ChatMessageIndexViewModel BuildViewModel(
        IReadOnlyList<ChatMessageAdminDto> all,
        string? search, string? typeFilter, string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(r => r.Content != null && r.Content.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(typeFilter))
            filtered = filtered.Where(r => string.Equals(r.MessageType ?? "Text", typeFilter, StringComparison.OrdinalIgnoreCase));

        if (activeFilter == "active")
            filtered = filtered.Where(r => r.IsActive);
        else if (activeFilter == "passive")
            filtered = filtered.Where(r => !r.IsActive);

        if (deletedFilter == "deleted")
            filtered = filtered.Where(r => r.IsDeleted);
        else if (deletedFilter == "notDeleted")
            filtered = filtered.Where(r => !r.IsDeleted);

        if (DateTime.TryParse(dateFrom, out var from))
            filtered = filtered.Where(r => r.CreatedAt >= from);
        if (DateTime.TryParse(dateTo, out var to))
            filtered = filtered.Where(r => r.CreatedAt < to.AddDays(1));

        var filteredList = filtered.OrderByDescending(r => r.CreatedAt).ToList();
        var totalFiltered = filteredList.Count;

        var safePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var safePageNumber = pageNumber > 0 ? pageNumber : 1;

        var rows = filteredList.Skip((safePageNumber - 1) * safePageSize).Take(safePageSize).ToList();

        return new ChatMessageIndexViewModel
        {
            Rows = rows,
            TotalCount = all.Count,
            ActiveCount = all.Count(r => r.IsActive && !r.IsDeleted),
            PassiveCount = all.Count(r => !r.IsActive && !r.IsDeleted),
            DeletedCount = all.Count(r => r.IsDeleted),
            SearchContent = search,
            TypeFilter = typeFilter,
            ActiveFilter = activeFilter,
            DeletedFilter = deletedFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalFiltered = totalFiltered
        };
    }

    public async Task<IActionResult> Index(string? search = null, string? typeFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var all = await _chatMessageApiClient.GetAllForAdminAsync(token, cancellationToken);
        return View(BuildViewModel(all, search, typeFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? search = null, string? typeFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var all = await _chatMessageApiClient.GetAllForAdminAsync(token, cancellationToken);
        return PartialView("_ChatMessageTable", BuildViewModel(all, search, typeFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    /// <summary>
    /// Sohbet ekini (ses/foto/video) API'nin yetkili ucundan akış olarak sunar.
    /// Ekler API'de statik sunulmadığından admin önizlemesi bu proxy üzerinden çalışır.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Attachment(string file, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(file)) return BadRequest();

        var (stream, contentType) = await _chatMessageApiClient.GetAttachmentAsync(file, token, cancellationToken);
        if (stream is null) return NotFound();

        return File(stream, contentType ?? "application/octet-stream", enableRangeProcessing: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _chatMessageApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Durum değiştirme işlemi başarısız oldu." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var ok = await _chatMessageApiClient.RestoreAsync(id, token, cancellationToken);
        return ok ? Ok() : StatusCode(500, new { message = "Geri yükleme işlemi başarısız oldu." });
    }
}
