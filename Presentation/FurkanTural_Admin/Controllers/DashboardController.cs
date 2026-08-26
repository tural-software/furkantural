using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Dashboard;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class DashboardController(IAdminSummaryClient summaryClient) : Controller
{
    private readonly IAdminSummaryClient _summaryClient = summaryClient;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var modules = AdminModules.All;
        var summaries = await Task.WhenAll(
            modules.Select(m => _summaryClient.GetAsync(m.ApiPath, token, cancellationToken)));

        var cards = new Dictionary<string, EntityCardViewModel>(modules.Count);
        for (var i = 0; i < modules.Count; i++)
        {
            var m = modules[i];
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
                CountUnitLabel = m.CountUnitLabel
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
            TotalRecordCount = known.Count == 0 ? null : known.Sum(s => s!.TotalCount)
        };

        return View(vm);
    }
}
