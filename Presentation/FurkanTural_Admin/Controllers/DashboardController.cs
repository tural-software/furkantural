using System.Globalization;
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
        new("music-images", "musicimage", "Müzikler Görselleri",
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
            "kayıt"),
        new("projects", "project", "Projeler",
            "Proje kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "proje"),
        new("project-images", "projectimage", "Proje Görselleri",
            "Projelere bağlı görselleri görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "görsel"),
        new("roles", "role", "Roller",
            "Kullanıcı rollerini görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "rol")
    ];

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Auth");

        var summaries = await Task.WhenAll(
            Modules.Select(m => _summaryClient.GetAsync(m.ApiPath, token, cancellationToken)));

        var trCulture = CultureInfo.GetCultureInfo("tr-TR");
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
                IsLocked = m.Slug != "subscribers" && m.Slug != "skills" && m.Slug != "blogs" && m.Slug != "blog-images" && m.Slug != "experiences" && m.Slug != "educations" && m.Slug != "logs" && m.Slug != "users" && m.Slug != "music",
                ManageUrl = m.Slug == "subscribers" ? Url.Action("Index", "Subscriber")
                          : m.Slug == "skills"       ? Url.Action("Index", "Skill")
                          : m.Slug == "blogs"        ? Url.Action("Index", "Blog")
                          : m.Slug == "blog-images"  ? Url.Action("Index", "BlogImage")
                          : m.Slug == "experiences"  ? Url.Action("Index", "Experience")
                          : m.Slug == "educations"   ? Url.Action("Index", "Education")
                          : m.Slug == "logs"          ? Url.Action("Index", "Log")
                          : m.Slug == "users"         ? Url.Action("Index", "User")
                          : m.Slug == "music"          ? Url.Action("Index", "Music")
                          : null,
                CountUnitLabel = m.CountUnitLabel
            })
            .OrderBy(c => c.Title, StringComparer.Create(trCulture, ignoreCase: false))
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
