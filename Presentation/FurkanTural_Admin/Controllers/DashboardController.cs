using System.Globalization;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Dashboard;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class DashboardController(IAdminDashboardClient dashboardClient) : Controller
{
    public const int WindowDays = 7;

    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    private readonly IAdminDashboardClient _dashboardClient = dashboardClient;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var data = await _dashboardClient.GetAsync(WindowDays, token, cancellationToken);
        var summaries = data?.Summaries ?? new Dictionary<string, EntitySummaryModel>(StringComparer.OrdinalIgnoreCase);

        var attention = new List<AttentionItemViewModel>();
        if (data?.UnreadContacts is > 0)
            attention.Add(new AttentionItemViewModel("contact", "okunmamış", data.UnreadContacts, "okunmamış iletişim mesajı",
                Url.Action("Index", "Contact", new { readFilter = "unread" })));
        if (data?.PendingReports is > 0)
            attention.Add(new AttentionItemViewModel("reports", "bekleyen", data.PendingReports, "bekleyen şikayet",
                Url.Action("Index", "Report", new { statusFilter = "Pending" })));
        var attentionBySlug = attention.ToDictionary(a => a.Slug);

        var modules = AdminModules.All;
        var cards = new Dictionary<string, EntityCardViewModel>(modules.Count);
        foreach (var m in modules)
        {
            summaries.TryGetValue(m.ApiPath, out var summary);
            attentionBySlug.TryGetValue(m.Slug, out var flag);
            cards[m.Slug] = new EntityCardViewModel
            {
                Slug = m.Slug,
                Title = m.Title,
                Description = m.Description,
                IconKey = m.Slug,
                Actions = m.Actions,
                TotalCount = summary?.TotalCount,
                LastActivityAt = summary?.LastActivityAt,
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

        var totalRecords = summaries.Count == 0 ? (int?)null : summaries.Values.Sum(s => s.TotalCount);

        var vm = new DashboardViewModel
        {
            Username = HttpContext.Session.GetString("username"),
            Groups = groups,
            TotalRecordCount = totalRecords,
            Attention = attention,
            Kpis = BuildKpis(data, totalRecords)
        };

        return View(vm);
    }

    private IReadOnlyList<KpiViewModel> BuildKpis(AdminDashboardModel? data, int? totalRecords)
    {
        var thisWeek = data?.ThisWeek;
        var lastWeek = data?.LastWeek;
        var freshTotal = thisWeek is null ? (int?)null : thisWeek.Blogs + thisWeek.Users + thisWeek.Contacts + thisWeek.Subscribers;
        var lastTotal = lastWeek is null ? (int?)null : lastWeek.Blogs + lastWeek.Users + lastWeek.Contacts + lastWeek.Subscribers;
        var freshDetail = thisWeek is null
            ? null
            : $"{Format(thisWeek.Blogs)} yazı · {Format(thisWeek.Users)} kullanıcı · {Format(thisWeek.Contacts)} mesaj · {Format(thisWeek.Subscribers)} abone";
        var trend = freshTotal is { } now && lastTotal is { } before ? now - before : (int?)null;

        var open = data is null ? (int?)null : data.UnreadContacts + data.PendingReports;
        var openDetail = data is null
            ? null
            : $"{Format(data.UnreadContacts)} okunmamış mesaj · {Format(data.PendingReports)} bekleyen şikayet";

        return
        [
            new KpiViewModel("records", "Toplam kayıt", Format(totalRecords), $"{AdminModules.All.Count} modül", null, null, null),
            new KpiViewModel("fresh", "Bu hafta yeni", Format(freshTotal), freshDetail, trend,
                lastTotal is null ? null : $"geçen hafta {Format(lastTotal)}", null),
            new KpiViewModel("active-users", "Aktif kullanıcı", Format(data?.ActiveUsers), $"son {WindowDays} günde görülen", null, null,
                Url.Action("Index", "User")),
            new KpiViewModel("open-work", "Bekleyen iş", Format(open), openDetail, null, null,
                open is > 0 ? "#dash-attention-title" : null)
        ];
    }

    private static string Format(int? value)
        => value.HasValue ? value.Value.ToString("N0", TrCulture) : "—";
}
