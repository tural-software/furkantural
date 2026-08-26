namespace FurkanTural_Domain.Constants;

/// <summary>Tablo şeması ucunun okumasına izin verilen entity adları. Uç bir istek parametresini EF modelinde arattığı için bu liste bir güvenlik sınırıdır: burada olmayan her ad 404 döner, aksi hâlde uç modeldeki her tipi yoklamak için kullanılabilirdi.<para>Liste yönetim panelinin yönettiği yirmi bir modülle birebir örtüşür; panelde karşılığı olmayan entity buraya eklenmez.</para></summary>
public static class SchemaEntityDefinitions
{
    public const string Blog = "Blog";
    public const string BlogImage = "BlogImage";
    public const string Category = "Category";
    public const string Project = "Project";
    public const string ProjectImage = "ProjectImage";
    public const string Music = "Music";
    public const string MusicImage = "MusicImage";
    public const string Skill = "Skill";
    public const string Experience = "Experience";
    public const string Education = "Education";
    public const string User = "User";
    public const string UserFriend = "UserFriend";
    public const string ChatMessage = "ChatMessage";
    public const string CallLog = "CallLog";
    public const string Report = "Report";
    public const string Contact = "Contact";
    public const string MailTemplate = "MailTemplate";
    public const string Subscriber = "Subscriber";
    public const string Role = "Role";
    public const string Status = "Status";
    public const string Log = "Log";

    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        Blog, BlogImage, Category, Project, ProjectImage, Music, MusicImage,
        Skill, Experience, Education,
        User, UserFriend, ChatMessage, CallLog, Report,
        Contact, MailTemplate, Subscriber,
        Role, Status, Log
    };

    public static IReadOnlyCollection<string> All => Allowed;

    public static bool IsAllowed(string? entity) =>
        !string.IsNullOrWhiteSpace(entity) && Allowed.Contains(entity);
}
