using System.Globalization;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Dashboard;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class DashboardController(
    IAdminSummaryClient summaryClient,
    IContactApiClient contactApiClient,
    IReportApiClient reportApiClient,
    IUserApiClient userApiClient,
    IBlogApiClient blogApiClient,
    ISubscriberApiClient subscriberApiClient) : Controller
{
    private const int WindowDays = 7;

    private readonly IAdminSummaryClient _summaryClient = summaryClient;
    private readonly IContactApiClient _contactApiClient = contactApiClient;
    private readonly IReportApiClient _reportApiClient = reportApiClient;
    private readonly IUserApiClient _userApiClient = userApiClient;
    private readonly IBlogApiClient _blogApiClient = blogApiClient;
    private readonly ISubscriberApiClient _subscriberApiClient = subscriberApiClient;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var today = DateTime.UtcNow.Date;
        var thisWeek = Window(today.AddDays(-(WindowDays - 1)), null);
        var lastWeek = Window(today.AddDays(-(2 * WindowDays - 1)), today.AddDays(-WindowDays));
        var seenSince = new AdminListRequest { IsDeleted = false, IsActive = true }
            .With("seenSince", today.AddDays(-(WindowDays - 1)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        var modules = AdminModules.All;
        var summaryTasks = modules.Select(m => _summaryClient.GetAsync(m.ApiPath, token, cancellationToken)).ToArray();
        var unreadTask = _contactApiClient.GetAdminCountsAsync(new AdminListRequest { IsDeleted = false }.With("isRead", false), token, cancellationToken);
        var pendingTask = _reportApiClient.GetAdminCountsAsync(new AdminListRequest { IsDeleted = false }.With("status", "Pending"), token, cancellationToken);
        var activeUsersTask = _userApiClient.GetAdminCountsAsync(seenSince, token, cancellationToken);
        var fresh = new (string Unit, Task<StatusCountsModel?> This, Task<StatusCountsModel?> Last)[]
        {
            ("yazı", _blogApiClient.GetAdminCountsAsync(thisWeek, token, cancellationToken), _blogApiClient.GetAdminCountsAsync(lastWeek, token, cancellationToken)),
            ("kullanıcı", _userApiClient.GetAdminCountsAsync(thisWeek, token, cancellationToken), _userApiClient.GetAdminCountsAsync(lastWeek, token, cancellationToken)),
            ("mesaj", _contactApiClient.GetAdminCountsAsync(thisWeek, token, cancellationToken), _contactApiClient.GetAdminCountsAsync(lastWeek, token, cancellationToken)),
            ("abone", _subscriberApiClient.GetAdminCountsAsync(thisWeek, token, cancellationToken), _subscriberApiClient.GetAdminCountsAsync(lastWeek, token, cancellationToken))
        };
        await Task.WhenAll([.. summaryTasks, unreadTask, pendingTask, activeUsersTask, .. fresh.Select(f => f.This), .. fresh.Select(f => f.Last)]);

        var summaries = summaryTasks.Select(t => t.Result).ToArray();
        var unread = unreadTask.Result?.Total;
        var pending = pendingTask.Result?.Total;

        var attention = new List<AttentionItemViewModel>();
        if (unread is > 0)
            attention.Add(new AttentionItemViewModel("contact", "okunmamış", unread.Value, "okunmamış iletişim mesajı",
                Url.Action("Index", "Contact", new { readFilter = "unread" })));
        if (pending is > 0)
            attention.Add(new AttentionItemViewModel("reports", "bekleyen", pending.Value, "bekleyen şikayet",
                Url.Action("Index", "Report", new { statusFilter = "Pending" })));
        var attentionBySlug = attention.ToDictionary(a => a.Slug);

        var cards = new Dictionary<string, EntityCardViewModel>(modules.Count);
        for (var i = 0; i < modules.Count; i++)
        {
            var m = modules[i];
            attentionBySlug.TryGetValue(m.Slug, out var flag);
            cards[m.Slug] = new EntityCardViewModel
            {
                Slug = m.Slug,
                Title = m.Title,
                Description = m.Description,
                IconKey = m.Slug,
                Actions = m.Actions,
                TotalCount = summaries[i]?.TotalCount,
                LastActivityAt = summaries[i]?.LastActivityAt,
                IsLocked = !m.IsReady,
                ManageUrl = m.IsReady ? Url.Action("Index", m.Controller) : null,
                CountUnitLabel = m.CountUnitLabel,
                AttentionCount = flag?.Count,
                AttentionLabel = flag?.Title,
                AttentionUrl = flag?.Url
            };
        }

        var groups = AdminModules.Grouped()
            .Select(g => new DashboardGroupViewModel
            {
                Name = g.Key,
                Modules = [.. g.Select(m => cards[m.Slug])]
            })
            .ToList();

        var known = summaries.Where(s => s is not null).ToList();
        var totalRecords = known.Count == 0 ? (int?)null : known.Sum(s => s!.TotalCount);

        var vm = new DashboardViewModel
        {
            Username = HttpContext.Session.GetString("username"),
            Groups = groups,
            TotalRecordCount = totalRecords,
            Attention = attention,
            Kpis = BuildKpis(totalRecords, fresh, activeUsersTask.Result?.Total, unread, pending)
        };

        return View(vm);
    }

    private static AdminListRequest Window(DateTime from, DateTime? to)
        => AdminListRequest.From(null, null, "notDeleted",
            from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 1, 10);

    private IReadOnlyList<KpiViewModel> BuildKpis(
        int? totalRecords,
        (string Unit, Task<StatusCountsModel?> This, Task<StatusCountsModel?> Last)[] fresh,
        int? activeUsers,
        int? unread,
        int? pending)
    {
        var thisWeek = fresh.Select(f => f.This.Result?.Total).ToArray();
        var lastWeek = fresh.Select(f => f.Last.Result?.Total).ToArray();
        var freshTotal = thisWeek.Any(v => v.HasValue) ? thisWeek.Sum(v => v ?? 0) : (int?)null;
        var lastTotal = lastWeek.Any(v => v.HasValue) ? lastWeek.Sum(v => v ?? 0) : (int?)null;
        var freshDetail = freshTotal is null
            ? null
            : string.Join(" · ", fresh.Select((f, i) => $"{Format(thisWeek[i])} {f.Unit}"));
        var trend = freshTotal is { } now && lastTotal is { } before ? now - before : (int?)null;

        var open = unread is null && pending is null ? (int?)null : (unread ?? 0) + (pending ?? 0);
        var openDetail = open is null
            ? null
            : $"{Format(unread)} okunmamış mesaj · {Format(pending)} bekleyen şikayet";

        return
        [
            new KpiViewModel("records", "Toplam kayıt", Format(totalRecords), $"{AdminModules.All.Count} modül", null, null, null),
            new KpiViewModel("fresh", $"Bu hafta yeni", Format(freshTotal), freshDetail, trend,
                lastTotal is null ? null : $"geçen hafta {lastTotal.Value.ToString("N0", TrCulture)}", null),
            new KpiViewModel("active-users", $"Aktif kullanıcı", Format(activeUsers), $"son {WindowDays} günde görülen", null, null,
                Url.Action("Index", "User")),
            new KpiViewModel("open-work", "Bekleyen iş", Format(open), openDetail, null, null,
                open is > 0 ? "#dash-attention-title" : null)
        ];
    }

    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static string Format(int? value)
        => value.HasValue ? value.Value.ToString("N0", TrCulture) : "—";
}
