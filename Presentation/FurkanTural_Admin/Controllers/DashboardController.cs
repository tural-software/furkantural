using System.Globalization;
using FurkanTural_Admin.Models.Dashboard;
using FurkanTural_Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class DashboardController(IAdminSummaryClient summaryClient) : Controller
{
    private readonly IAdminSummaryClient _summaryClient = summaryClient;

    /// <summary>Kullanıcı arayüzü tamamlanmış (unlock edilmiş) modüller. Yeni bir modül hazır hale geldiğinde bu listeye slug'ını ekle.</summary>
    private static readonly HashSet<string> ImplementedModules =
    [
        "subscribers",
        "skills",
        "blogs",
        "blog-images",
        "categories",
        "experiences",
        "educations",
        "logs",
        "users",
        "music",
        "music-images",
        "projects",
        "project-images",
        "roles",
        "contact",
        "mail-template",
        "statuses",
        "friends",
        "messages",
        "calls",
        "reports",
    ];

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
        new("categories", "category", "Kategoriler",
            "Blog kategorilerini görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "kategori"),
        new("logs", "log", "Kayıtlar",
            "Sistem loglarını görüntüleyin. Salt okunur kayıtlardır.",
            [EntityAction.Read],
            "kayıt"),
        new("projects", "project", "Projeler",
            "Proje kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "proje"),
        new("project-images", "projectimage", "Projeler Görselleri",
            "Projelere bağlı görselleri görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "görsel"),
        new("roles", "role", "Roller",
            "Kullanıcı rollerini görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "rol"),
        new("contact", "contact", "İletişim Mesajları",
            "Ziyaretçi mesajlarını görüntüleyin ve yönetin.",
            [EntityAction.Read, EntityAction.Delete],
            "mesaj"),
        new("mail-template", "mailtemplate", "Posta Şablonları",
            "E-posta şablonlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "şablon"),
        new("statuses", "status", "Durumlar",
            "Site geneli durum (status) kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete],
            "durum"),
        new("friends", "friend", "Arkadaşlıklar",
            "Kullanıcı arkadaşlık ilişkilerini görüntüleyin ve denetleyin.",
            [EntityAction.Read],
            "ilişki"),
        new("messages", "message", "Mesajlar",
            "Sohbet mesajlarını görüntüleyin ve denetleyin.",
            [EntityAction.Read],
            "mesaj"),
        new("calls", "call", "Aramalar",
            "Sesli/görüntülü arama kayıtlarını görüntüleyin ve denetleyin.",
            [EntityAction.Read],
            "arama"),
        new("reports", "report", "Şikayetler",
            "Kullanıcı/mesaj/medya/arama şikayetlerini inceleyin ve yönetin.",
            [EntityAction.Read],
            "şikayet")
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
                IsLocked = !ImplementedModules.Contains(m.Slug),
                ManageUrl = m.Slug == "subscribers" ? Url.Action("Index", "Subscriber")
                          : m.Slug == "skills"       ? Url.Action("Index", "Skill")
                          : m.Slug == "blogs"        ? Url.Action("Index", "Blog")
                          : m.Slug == "blog-images"  ? Url.Action("Index", "BlogImage")
                          : m.Slug == "categories"   ? Url.Action("Index", "Category")
                          : m.Slug == "experiences"  ? Url.Action("Index", "Experience")
                          : m.Slug == "educations"   ? Url.Action("Index", "Education")
                          : m.Slug == "logs"          ? Url.Action("Index", "Log")
                          : m.Slug == "users"         ? Url.Action("Index", "User")
                          : m.Slug == "music"          ? Url.Action("Index", "Music")
                          : m.Slug == "music-images"   ? Url.Action("Index", "MusicImage")
                          : m.Slug == "projects"       ? Url.Action("Index", "Project")
                          : m.Slug == "project-images" ? Url.Action("Index", "ProjectImage")
                          : m.Slug == "roles"            ? Url.Action("Index", "Role")
                          : m.Slug == "contact"           ? Url.Action("Index", "Contact")
                          : m.Slug == "mail-template"  ? Url.Action("Index", "MailTemplate")
                          : m.Slug == "statuses"          ? Url.Action("Index", "Status")
                          : m.Slug == "friends"           ? Url.Action("Index", "UserFriend")
                          : m.Slug == "messages"          ? Url.Action("Index", "ChatMessage")
                          : m.Slug == "calls"             ? Url.Action("Index", "CallLog")
                          : m.Slug == "reports"           ? Url.Action("Index", "Report")
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
