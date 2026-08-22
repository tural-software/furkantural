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
        string? search, string? usernameFilter, string? typeFilter, string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize)
    {
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(r => r.Content != null && r.Content.Contains(search, StringComparison.OrdinalIgnoreCase));

        // Filtre iki alanı birden tarar: eşleşme gönderende ya da alıcıda olabilir.
        if (!string.IsNullOrWhiteSpace(usernameFilter))
            filtered = filtered.Where(r =>
                (r.SenderUsername != null && r.SenderUsername.Contains(usernameFilter, StringComparison.OrdinalIgnoreCase)) ||
                (r.ReceiverUsername != null && r.ReceiverUsername.Contains(usernameFilter, StringComparison.OrdinalIgnoreCase)));

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
            UsernameFilter = usernameFilter,
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

    public async Task<IActionResult> Index(string? search = null, string? usernameFilter = null, string? typeFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var all = await _chatMessageApiClient.GetAllForAdminAsync(token, cancellationToken);
        return View(BuildViewModel(all, search, usernameFilter, typeFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    public IActionResult TableDetail()
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? search = null, string? usernameFilter = null, string? typeFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        var all = await _chatMessageApiClient.GetAllForAdminAsync(token, cancellationToken);
        return PartialView("_ChatMessageTable", BuildViewModel(all, search, usernameFilter, typeFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize));
    }

    /// <summary>
    /// Sohbet ekini API'nin yetkili ucundan akış olarak sunar. Ekler API'de statik sunulmadığı için
    /// panel önizlemesi bu vekil üzerinden çalışır.
    ///
    /// Dosya adı API'ye geçmeden önce burada da elenir: üst dizine çıkma işaretleri ve yol
    /// ayraçları reddedilir, uzantı beyaz listeye sokulur. Asıl yetki denetimini API yapar; buradaki
    /// eleme onun yerine geçmez, adres çubuğundan gelen bir değerin ağa çıkmasını engeller.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Attachment(string file, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(file)) return BadRequest();

        if (file.Contains("..") || file.Contains('/') || file.Contains('\\'))
            return BadRequest();

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp3", ".ogg", ".wav", ".m4a", ".mp4", ".webm", ".mov" };
        var ext = Path.GetExtension(file);
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            return BadRequest();

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