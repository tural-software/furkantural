using FurkanTural_Admin.Models.Navigation;

namespace FurkanTural_Admin.Helpers;

/// <summary>Panelde bir kaydın alt kayıtlarına götüren ilişkiler. Yalnızca listede zaten bir geçiş düğmesi bulunan, yani süzgeci çalışan ilişkiler burada durur — uydurma bağlantı verilmez.</summary>
public static class AdminRelations
{
    private static readonly AdminRelation[] All =
    [
        new("Blog", "BlogImage", "blogId"),
        new("Music", "MusicImage", "musicId"),
        new("Project", "ProjectImage", "projectId"),
        new("Role", "User", "roleId")
    ];

    public static IReadOnlyList<AdminRelation> For(string? parentEntity) =>
        parentEntity is null
            ? []
            : [.. All.Where(r => string.Equals(r.ParentEntity, parentEntity, StringComparison.OrdinalIgnoreCase))];
}
