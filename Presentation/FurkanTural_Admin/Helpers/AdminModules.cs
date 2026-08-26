using FurkanTural_Admin.Models.Dashboard;
using FurkanTural_Admin.Models.Navigation;

namespace FurkanTural_Admin.Helpers;

/// <summary>Yirmi bir yönetim modülünün tek kaydı. Ana ekran ızgarası, modül seçici ve kırıntı yolu buradan okur; modül adı, grubu ve controller'ı başka hiçbir yerde yazılı değildir.</summary>
public static class AdminModules
{
    public const string GroupContent = "İçerik";
    public const string GroupProfile = "Profil";
    public const string GroupCommunity = "Topluluk";
    public const string GroupContact = "İletişim";
    public const string GroupSystem = "Sistem";

    private static readonly string[] GroupOrder =
        [GroupContent, GroupProfile, GroupCommunity, GroupContact, GroupSystem];

    private static readonly EntityAction[] Crud =
        [EntityAction.Create, EntityAction.Read, EntityAction.Update, EntityAction.Delete];

    private static readonly EntityAction[] ReadOnly = [EntityAction.Read];

    public static readonly IReadOnlyList<AdminModule> All =
    [
        new("blogs", "blog", "Blog", "Blog", "Blog Yazıları",
            "Blog yazılarını görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupContent, Crud, "yazı"),
        new("blog-images", "blogimage", "BlogImage", "BlogImage", "Blog Görselleri",
            "Blog yazılarına bağlı görselleri görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupContent, Crud, "görsel"),
        new("categories", "category", "Category", "Category", "Kategoriler",
            "Blog kategorilerini görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupContent, Crud, "kategori"),
        new("projects", "project", "Project", "Project", "Projeler",
            "Proje kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupContent, Crud, "proje"),
        new("project-images", "projectimage", "ProjectImage", "ProjectImage", "Proje Görselleri",
            "Projelere bağlı görselleri görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupContent, Crud, "görsel"),
        new("music", "music", "Music", "Music", "Müzikler",
            "Müzik kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupContent, Crud, "müzik"),
        new("music-images", "musicimage", "MusicImage", "MusicImage", "Müzik Görselleri",
            "Müziklere bağlı görselleri görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupContent, Crud, "görsel"),

        new("skills", "skill", "Skill", "Skill", "Beceriler",
            "Yetkinlik kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupProfile, Crud, "beceri"),
        new("experiences", "experience", "Experience", "Experience", "Deneyimler",
            "İş tecrübelerinizi görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupProfile, Crud, "deneyim"),
        new("educations", "education", "Education", "Education", "Eğitimler",
            "Eğitim bilgilerini görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupProfile, Crud, "eğitim"),

        new("users", "user", "User", "User", "Kullanıcılar",
            "Kullanıcı kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupCommunity, Crud, "kullanıcı"),
        new("friends", "friend", "UserFriend", "UserFriend", "Arkadaşlıklar",
            "Kullanıcı arkadaşlık ilişkilerini görüntüleyin ve denetleyin.",
            GroupCommunity, ReadOnly, "ilişki"),
        new("messages", "message", "ChatMessage", "ChatMessage", "Mesajlar",
            "Sohbet mesajlarını görüntüleyin ve denetleyin.",
            GroupCommunity, ReadOnly, "mesaj"),
        new("calls", "call", "CallLog", "CallLog", "Aramalar",
            "Sesli/görüntülü arama kayıtlarını görüntüleyin ve denetleyin.",
            GroupCommunity, ReadOnly, "arama"),
        new("reports", "report", "Report", "Report", "Şikayetler",
            "Kullanıcı/mesaj/medya/arama şikayetlerini inceleyin ve yönetin.",
            GroupCommunity, ReadOnly, "şikayet"),

        new("contact", "contact", "Contact", "Contact", "İletişim Mesajları",
            "Ziyaretçi mesajlarını görüntüleyin ve yönetin.",
            GroupContact, [EntityAction.Read, EntityAction.Delete], "mesaj"),
        new("mail-template", "mailtemplate", "MailTemplate", "MailTemplate", "Posta Şablonları",
            "E-posta şablonlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupContact, Crud, "şablon"),
        new("subscribers", "subscriber", "Subscriber", "Subscriber", "Aboneler",
            "Bülten abonelerini görüntüleyin, ekleyin ve silin.",
            GroupContact, [EntityAction.Create, EntityAction.Read, EntityAction.Delete], "abone"),

        new("roles", "role", "Role", "Role", "Roller",
            "Kullanıcı rollerini görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupSystem, Crud, "rol"),
        new("statuses", "status", "Status", "Status", "Durumlar",
            "Site geneli durum (status) kayıtlarını görüntüleyin ve CRUD işlemlerini yönetin.",
            GroupSystem, Crud, "durum"),
        new("logs", "log", "Log", "Log", "Sistem Kayıtları",
            "Sistem kayıtlarını görüntüleyin. Salt okunur kayıtlardır.",
            GroupSystem, ReadOnly, "kayıt")
    ];

    private static readonly Dictionary<string, AdminModule> ByControllerName =
        All.ToDictionary(m => m.Controller, StringComparer.OrdinalIgnoreCase);

    /// <summary>Controller adından modülü bulur. Kırıntı yolu bu yolla çalışır: view kendi adını bilmez, route'tan gelen controller adı yeter.</summary>
    public static AdminModule? ByController(string? controller) =>
        controller is not null && ByControllerName.TryGetValue(controller, out var module) ? module : null;

    /// <summary>Modülleri grup sırasına göre, grup içinde bildirim sırasını koruyarak döndürür.</summary>
    public static IReadOnlyList<IGrouping<string, AdminModule>> Grouped() =>
        [.. All.GroupBy(m => m.Group).OrderBy(g => Array.IndexOf(GroupOrder, g.Key))];
}
