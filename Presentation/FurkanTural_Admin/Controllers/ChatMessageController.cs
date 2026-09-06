using FurkanTural_Admin.Helpers;
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

    public async Task<IActionResult> Index(string? search = null, string? usernameFilter = null, string? typeFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        return View(await BuildViewModelAsync(token, search, usernameFilter, typeFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(string? search = null, string? usernameFilter = null, string? typeFilter = null, string? activeFilter = null,
        string? deletedFilter = null, string? dateFrom = null, string? dateTo = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        ViewData["ApiBaseUrl"] = _apiOptions.BaseUrl;
        return PartialView("_ChatMessageTable", await BuildViewModelAsync(token, search, usernameFilter, typeFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken));
    }

    private async Task<ChatMessageIndexViewModel> BuildViewModelAsync(
        string token,
        string? search, string? usernameFilter, string? typeFilter, string? activeFilter, string? deletedFilter,
        string? dateFrom, string? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(search, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize)
            .With("username", usernameFilter)
            .With("messageType", typeFilter);

        var countsTask = _chatMessageApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _chatMessageApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new ChatMessageIndexViewModel
        {
            Rows = rows,
            TotalCount = counts?.Total ?? 0,
            ActiveCount = counts?.Active ?? 0,
            PassiveCount = counts?.Passive ?? 0,
            DeletedCount = counts?.Deleted ?? 0,
            SearchContent = search,
            UsernameFilter = usernameFilter,
            TypeFilter = typeFilter,
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

    /// <summary>Sohbet ekini API'nin yetkili ucundan akış olarak sunar. Ekler API'de statik sunulmadığı için panel önizlemesi bu vekil üzerinden çalışır.<para>Dosya adı API'ye geçmeden önce burada da elenir: üst dizine çıkma işaretleri ve yol ayraçları reddedilir, uzantı beyaz listeye sokulur. Asıl yetki denetimini API yapar; buradaki eleme onun yerine geçmez, adres çubuğundan gelen bir değerin ağa çıkmasını engeller.</para></summary>
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
    public async Task<IActionResult> Bulk(string? action, int[]? ids, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var wanted = (ids ?? []).Where(i => i > 0).Distinct().ToList();
        if (string.IsNullOrWhiteSpace(action) || wanted.Count == 0)
            return BadRequest(new { message = "İşlem türü ve en az bir kayıt gerekir." });

        var result = await _chatMessageApiClient.BulkAsync(action.Trim().ToLowerInvariant(), wanted, token, cancellationToken);
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
