using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Comment;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

/// <summary>Yorum denetimi. Modülün diğerlerinden farkı, yeni kayıt açan bir formunun olmamasıdır: yorumu ziyaretçi yazar, burada verilen tek yeni satır yazının sahibinin yanıtıdır.<para>Gövdeyi düzenleyen bir uç da yok. Başkasının sözünü sessizce değiştirmek yerine karar onaylamak, reddetmek ya da silmektir.</para></summary>
public class CommentController(ICommentApiClient commentApiClient) : Controller
{
    private readonly ICommentApiClient _commentApiClient = commentApiClient;

    public async Task<IActionResult> Index(
        string? name,
        string? statusFilter,
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

        var vm = await BuildViewModelAsync(token, name, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? name,
        string? statusFilter,
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

        var vm = await BuildViewModelAsync(token, name, statusFilter, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_CommentTable", vm);
    }

    /// <summary>Denetim sayaçları süzgeçten bağımsız okunur. Bekleyen yorum sayısı, listede hangi süzgeç açık olursa olsun aynı kalmalıdır — süzgeçle birlikte değişen bir sayaç, sıfır göründüğünde kuyruğun boşaldığı izlenimini verirdi.</summary>
    private async Task<CommentIndexViewModel> BuildViewModelAsync(
        string token,
        string? name, string? statusFilter, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(name, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize)
            .With("status", NormalizeStatus(statusFilter));

        var countsTask = _commentApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var moderationTask = _commentApiClient.GetModerationCountsAsync(token, cancellationToken);
        var pagedTask = _commentApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        await Task.WhenAll(countsTask, moderationTask, pagedTask);

        var counts = await countsTask;
        var moderation = await moderationTask;
        var (rows, totalFiltered) = await pagedTask;

        return new CommentIndexViewModel
        {
            Rows = rows,
            TotalCount = counts?.Total ?? 0,
            ActiveCount = counts?.Active ?? 0,
            PassiveCount = counts?.Passive ?? 0,
            DeletedCount = counts?.Deleted ?? 0,
            PendingCount = moderation?.Pending ?? 0,
            ApprovedCount = moderation?.Approved ?? 0,
            RejectedCount = moderation?.Rejected ?? 0,
            SearchName = name,
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
    public async Task<IActionResult> SetStatus(int id, string? status, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var wanted = NormalizeStatus(status);
        if (wanted is null)
            return BadRequest(new { message = "Geçersiz yorum durumu." });

        var result = await _commentApiClient.SetStatusAsync(id, wanted, token, cancellationToken);
        return result.ToActionResult("Yorum durumu değiştirilemedi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, [FromForm] CommentReplyFormDto dto, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _commentApiClient.ReplyAsync(id, dto, token, cancellationToken);
        return result.ToActionResult("Yanıt gönderilemedi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _commentApiClient.DeleteAsync(id, token, cancellationToken);
        return result.ToActionResult("Silme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _commentApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return result.ToActionResult("Durum değiştirme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _commentApiClient.RestoreAsync(id, token, cancellationToken);
        return result.ToActionResult("Geri yükleme işlemi başarısız oldu.");
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

        var result = await _commentApiClient.BulkAsync(action.Trim().ToLowerInvariant(), wanted, token, cancellationToken);
        return result is null
            ? StatusCode(500, new { message = "Toplu işlem başarısız oldu." })
            : Json(new { requested = result.Requested, affected = result.Affected, skipped = result.Skipped });
    }

    /// <summary>Süzgeç ve karar değerlerini API'nin beklediği yazıma çevirir. Tanınmayan değer null döner ve süzgeç hiç uygulanmaz; kararda ise isteği reddettirir.</summary>
    private static string? NormalizeStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "pending" => "Pending",
        "approved" => "Approved",
        "rejected" => "Rejected",
        _ => null
    };
}
