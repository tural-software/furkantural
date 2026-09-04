using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Dashboard;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class DashboardController(IAdminSummaryClient summaryClient, IContactApiClient contactApiClient, IReportApiClient reportApiClient) : Controller
{
    private readonly IAdminSummaryClient _summaryClient = summaryClient;
    private readonly IContactApiClient _contactApiClient = contactApiClient;
    private readonly IReportApiClient _reportApiClient = reportApiClient;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var modules = AdminModules.All;
        var summaryTasks = modules.Select(m => _summaryClient.GetAsync(m.ApiPath, token, cancellationToken)).ToArray();
        var unreadTask = _contactApiClient.GetAdminCountsAsync(new AdminListRequest { IsDeleted = false }.With("isRead", false), token, cancellationToken);
        var pendingTask = _reportApiClient.GetAdminCountsAsync(new AdminListRequest { IsDeleted = false }.With("status", "Pending"), token, cancellationToken);
        await Task.WhenAll([.. summaryTasks, unreadTask, pendingTask]);

        var summaries = summaryTasks.Select(t => t.Result).ToArray();
        var attention = new List<AttentionItemViewModel>();
        var unread = (await unreadTask)?.Total;
        var pending = (await pendingTask)?.Total;
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

        var vm = new DashboardViewModel
        {
            Username = HttpContext.Session.GetString("username"),
            Groups = groups,
            TotalRecordCount = known.Count == 0 ? null : known.Sum(s => s!.TotalCount),
            Attention = attention
        };

        return View(vm);
    }
}
