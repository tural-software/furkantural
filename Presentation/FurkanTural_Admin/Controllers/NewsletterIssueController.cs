using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.NewsletterIssue;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class NewsletterIssueController(INewsletterIssueApiClient newsletterIssueApiClient) : Controller
{
    private readonly INewsletterIssueApiClient _newsletterIssueApiClient = newsletterIssueApiClient;

    public async Task<IActionResult> Index(
        string? subject,
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

        var vm = await BuildViewModelAsync(token, subject, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TablePartial(
        string? subject,
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

        var vm = await BuildViewModelAsync(token, subject, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize, cancellationToken);
        return PartialView("_NewsletterIssueTable", vm);
    }

    private async Task<NewsletterIssueIndexViewModel> BuildViewModelAsync(
        string token,
        string? subject, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var request = AdminListRequest.From(subject, activeFilter, deletedFilter, dateFrom, dateTo, pageNumber, pageSize);

        var countsTask = _newsletterIssueApiClient.GetAdminCountsAsync(AdminListRequest.Unfiltered, token, cancellationToken);
        var pagedTask = _newsletterIssueApiClient.GetAdminPagedAsync(request, token, cancellationToken);
        var audienceTask = _newsletterIssueApiClient.GetAudienceCountAsync(token, cancellationToken);
        await Task.WhenAll(countsTask, pagedTask, audienceTask);

        var counts = await countsTask;
        var (rows, totalFiltered) = await pagedTask;

        return new NewsletterIssueIndexViewModel
        {
            Rows = rows,
            TotalCount = counts?.Total ?? 0,
            ActiveCount = counts?.Active ?? 0,
            PassiveCount = counts?.Passive ?? 0,
            DeletedCount = counts?.Deleted ?? 0,
            ConfirmedSubscriberCount = await audienceTask,
            SearchSubject = subject,
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
    public async Task<IActionResult> Create([FromForm] NewsletterIssueFormDto dto, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _newsletterIssueApiClient.CreateAsync(dto, token, cancellationToken);
        return result.ToActionResult("Bülten oluşturulurken bir hata oluştu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [FromForm] NewsletterIssueFormDto dto, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _newsletterIssueApiClient.UpdateAsync(id, dto, token, cancellationToken);
        return result.ToActionResult("Güncelleme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _newsletterIssueApiClient.DeleteAsync(id, token, cancellationToken);
        return result.ToActionResult("Silme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _newsletterIssueApiClient.ToggleActiveAsync(id, token, cancellationToken);
        return result.ToActionResult("Durum değiştirme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _newsletterIssueApiClient.RestoreAsync(id, token, cancellationToken);
        return result.ToActionResult("Geri yükleme işlemi başarısız oldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Queue(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _newsletterIssueApiClient.QueueAsync(id, token, cancellationToken);
        return result.ToActionResult("Bülten dağıtıma verilemedi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTest(int id, string? email, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var result = await _newsletterIssueApiClient.SendTestAsync(id, email, token, cancellationToken);
        return result.ToActionResult("Deneme gönderilemedi.");
    }

    [HttpGet]
    public async Task<IActionResult> Progress(int id, CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var progress = await _newsletterIssueApiClient.GetProgressAsync(id, token, cancellationToken);
        return progress is null
            ? StatusCode(500, new { message = "İlerleme bilgisi alınamadı." })
            : Json(progress);
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

        var result = await _newsletterIssueApiClient.BulkAsync(action.Trim().ToLowerInvariant(), wanted, token, cancellationToken);
        return result is null
            ? StatusCode(500, new { message = "Toplu işlem başarısız oldu." })
            : Json(new { requested = result.Requested, affected = result.Affected, skipped = result.Skipped });
    }
}
