namespace FurkanTural_Browser.Tests.Infrastructure;

public enum Access
{
    Public,
    ChatUser,
    AdminUser
}

public sealed record SiteApp(string Name, string EnvKey, string DefaultBaseUrl)
{
    public string BaseUrl =>
        Environment.GetEnvironmentVariable(EnvKey) is { Length: > 0 } custom ? custom.TrimEnd('/') : DefaultBaseUrl;

    public override string ToString() => Name;
}

public sealed record SitePage(
    SiteApp App,
    string Path,
    Access Access = Access.Public,
    string? Discover = null,
    string? DiscoverFrom = null)
{
    public string Id => $"{App.Name}{Path}";
    public override string ToString() => Id;
}

public static class SiteMap
{
    public static readonly SiteApp Api = new("API", "SWEEP_URL_API", "http://localhost:7000");
    public static readonly SiteApp Admin = new("Admin", "SWEEP_URL_ADMIN", "http://localhost:7001");
    public static readonly SiteApp Portfolio = new("Portfolio", "SWEEP_URL_PORTFOLIO", "http://localhost:7002");
    public static readonly SiteApp Blog = new("Blog", "SWEEP_URL_BLOG", "http://localhost:7003");
    public static readonly SiteApp Chat = new("Chat", "SWEEP_URL_CHAT", "http://localhost:7004");

    public static readonly IReadOnlyList<SiteApp> Apps = [Admin, Portfolio, Blog, Chat];

    private static readonly string[] AdminSections =
    [
        "Dashboard", "Blog", "BlogImage", "CallLog", "Category", "ChatMessage", "Contact",
        "Education", "Experience", "Log", "MailTemplate", "Music", "MusicImage", "Project",
        "ProjectImage", "Report", "Role", "Skill", "Status", "Subscriber", "User", "UserFriend"
    ];

    public static readonly IReadOnlyList<SitePage> Pages = BuildPages();

    private static List<SitePage> BuildPages()
    {
        var pages = new List<SitePage>
        {
            new(Chat, "/"),
            new(Chat, "/Home/Privacy"),
            new(Chat, "/Home/Agreement"),
            new(Chat, "/Account/Login"),
            new(Chat, "/Account/Register"),
            new(Chat, "/Account/Activate"),
            new(Chat, "/offline.html"),
            new(Chat, "/Chat", Access.ChatUser),
            new(Chat, "/Account/Close", Access.ChatUser),

            new(Blog, "/"),
            new(Blog, "/Home/Privacy"),
            new(Blog, "/hakkinda"),
            new(Blog, "/ara?q=ef"),
            new(Blog, "/Home/Post", Access.Public, Discover: "a[href*='/Home/Post/']", DiscoverFrom: "/"),
            new(Blog, "/kategori", Access.Public, Discover: "a[href*='/kategori/']", DiscoverFrom: "/"),

            new(Portfolio, "/"),
            new(Portfolio, "/Home/Privacy"),
            new(Portfolio, "/Projects/Detail", Access.Public, Discover: "a[href*='/Projects/Detail/']", DiscoverFrom: "/"),
            new(Portfolio, "/Music/Detail", Access.Public, Discover: "a[href*='/Music/Detail/']", DiscoverFrom: "/"),

            new(Admin, "/")
        };

        pages.AddRange(AdminSections.Select(s => new SitePage(Admin, $"/{s}", Access.AdminUser)));
        return pages;
    }
}
