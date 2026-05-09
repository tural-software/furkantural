using FurkanTural_Admin.Models.Dashboard;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class DashboardController(IAdminSummaryClient summaryClient) : Controller
{
    private readonly IAdminSummaryClient _summaryClient = summaryClient;

    private static readonly EntityModuleDescriptor[] Modules =
    [
        new("users", "user", "Kullanıcılar",
            "Kullanıcı kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "kullanıcı"),
        new("subscribers", "subscriber", "Aboneler",
            "Bülten abonelerini görüntüleyin, ekleyin ve silin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Delete],
            "abone"),
        new("skills", "skill", "Beceriler",
            "Yetkinlik kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "beceri"),
        new("experiences", "experience", "Deneyimler",
            "İş tecrübelerinizi görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "deneyim"),
        new("educations", "education", "Eğitimler",
            "Eğitim bilgilerini görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "eğitim"),
        new("music", "music", "Müzikler",
            "Müzik kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "müzik"),
        new("music-images", "musicimage", "Müzik Görselleri",
            "Müziklere bağlı görselleri görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "görsel"),
        new("blogs", "blog", "Blog Yazıları",
            "Blog yazılarını görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "yazı"),
        new("blog-images", "blogimage", "Blog Yazısı Görselleri",
            "Blog yazılarına bağlı görselleri görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "görsel"),
        new("logs", "log", "Kayıtlar",
            "Sistem loglarını görüntüleyin. Salt okunur kayıtlardır.",
            [EntityAction.Read],
            "kayıt")
    ];

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var summaries = await Task.WhenAll(
            Modules.Select(m => _summaryClient.GetAsync(m.ApiPath, token, cancellationToken)));

        var cards = Modules
            .Select((m, i) => new EntityCardViewModel
            {
                Slug = m.Slug,
                Title = m.Title,
                Description = m.Description,
                IconKey = m.Slug,
                Actions = m.Actions,
                TotalCount = summaries[i]?.TotalCount,
                LastActivityAt = summaries[i]?.LastActivityAt,
                IsLocked = true,
                ManageUrl = null,
                CountUnitLabel = m.CountUnitLabel
            })
            .ToList();

        var vm = new DashboardViewModel
        {
            Username = HttpContext.Session.GetString("username"),
            Modules = cards
        };

        return View(vm);
    }

    private sealed record EntityModuleDescriptor(
        string Slug,
        string ApiPath,
        string Title,
        string Description,
        IReadOnlyList<EntityAction> Actions,
        string CountUnitLabel);
}
