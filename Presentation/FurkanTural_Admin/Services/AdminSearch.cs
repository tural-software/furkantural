using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;

namespace FurkanTural_Admin.Services;

public sealed record SearchHit(int Id, string Label, string RouteKey, string RouteValue);

public sealed record SearchGroup(string Slug, string Title, string Controller, IReadOnlyList<SearchHit> Hits);

public interface IAdminSearch
{
    Task<IReadOnlyList<SearchGroup>> SearchAsync(string? query, string token, CancellationToken ct = default);
}

public sealed class AdminSearch(
    IBlogApiClient blogs,
    IMusicApiClient musics,
    IProjectApiClient projects,
    IRoleApiClient roles,
    ICategoryApiClient categories,
    ISkillApiClient skills,
    IExperienceApiClient experiences,
    IEducationApiClient educations,
    IUserApiClient users,
    IContactApiClient contacts,
    IMailTemplateApiClient mailTemplates,
    ISubscriberApiClient subscribers,
    IStatusApiClient statuses,
    ICallLogApiClient callLogs,
    IChatMessageApiClient chatMessages,
    IReportApiClient reports,
    IBlogImageApiClient blogImages,
    IProjectImageApiClient projectImages,
    IMusicImageApiClient musicImages,
    ILogApiClient logs) : IAdminSearch
{
    public const int MinLength = 2;
    public const int PerModule = 5;
    public const int LabelLength = 80;

    public async Task<IReadOnlyList<SearchGroup>> SearchAsync(string? query, string token, CancellationToken ct = default)
    {
        var term = (query ?? "").Trim();
        if (term.Length < MinLength) return [];

        var jobs = new (string Controller, Task<IReadOnlyList<SearchHit>> Hits)[]
        {
            ("Blog", Options(() => blogs.GetAdminOptionsAsync(term, PerModule, token, ct), "blogId", byId: true)),
            ("Music", Options(() => musics.GetAdminOptionsAsync(term, PerModule, token, ct), "musicId", byId: true)),
            ("Project", Options(() => projects.GetAdminOptionsAsync(term, PerModule, token, ct), "projectId", byId: true)),
            ("Role", Options(() => roles.GetAdminOptionsAsync(term, PerModule, token, ct), "name", byId: false)),
            ("Category", Paged(term, r => categories.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Name, "name")),
            ("Skill", Paged(term, r => skills.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Name, "name")),
            ("Experience", Paged(term, r => experiences.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Position, "position")),
            ("Education", Paged(term, r => educations.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Institution, "institution")),
            ("User", Paged(term, r => users.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Username, "searchUsername")),
            ("Contact", Paged(term, r => contacts.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Name, "name")),
            ("MailTemplate", Paged(term, r => mailTemplates.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Name, "name")),
            ("Subscriber", Paged(term, r => subscribers.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Email, "email")),
            ("Status", Paged(term, r => statuses.GetAdminPagedAsync(r, token, ct), x => x.Id, x => x.Name, "name")),
            ("CallLog", Paged(term, r => callLogs.GetAdminPagedAsync(r, token, ct), x => x.Id,
                x => $"{x.CallerName ?? "?"} → {x.CalleeName ?? "?"} · {x.CallType}", "search", _ => term)),
            ("ChatMessage", Paged(term, r => chatMessages.GetAdminPagedAsync((r with { Search = null }).With("username", term), token, ct), x => x.Id,
                x => $"{x.SenderUsername ?? "?"} → {x.ReceiverUsername ?? "?"} · {x.MessageType ?? "Text"}", "usernameFilter", _ => term)),
            ("Report", Paged(term, r => reports.GetAdminPagedAsync(r, token, ct), x => x.Id,
                x => string.IsNullOrWhiteSpace(x.Reason) ? $"Şikayet #{x.Id} · {x.Status}" : $"{x.Reason} · {x.Status}", "search", _ => term)),
            ("BlogImage", Paged(term, r => blogImages.GetAdminPagedAsync(r, token, ct), x => x.Id,
                x => $"{Caption(x.AltText, x.Url)} · Blog #{x.BlogId}", "url", _ => term)),
            ("ProjectImage", Paged(term, r => projectImages.GetAdminPagedAsync(r, token, ct), x => x.Id,
                x => $"{Caption(x.AltText, x.Url)} · Proje #{x.ProjectId}", "url", _ => term)),
            ("MusicImage", Paged(term, r => musicImages.GetAdminPagedAsync(r, token, ct), x => x.Id,
                x => $"{Caption(x.AltText, x.Url)} · Müzik #{x.MusicId}", "url", _ => term)),
            ("Log", Rows(() => logs.GetAdminPagedAsync(null, null, term, null, null, 1, PerModule, token, ct), x => x.Id,
                x => $"{x.Level} · {x.Message}", "searchMessage", _ => term))
        };
        await Task.WhenAll(jobs.Select(j => j.Hits));

        var groups = new List<SearchGroup>();
        foreach (var (controller, task) in jobs)
        {
            var hits = task.Result;
            if (hits.Count == 0) continue;
            var module = AdminModules.ByController(controller);
            groups.Add(new SearchGroup(module?.Slug ?? controller.ToLowerInvariant(), module?.Title ?? controller, controller, hits));
        }
        return groups;
    }

    private static async Task<IReadOnlyList<SearchHit>> Options(Func<Task<IReadOnlyList<AdminOptionDto>>> fetch, string key, bool byId)
    {
        try
        {
            var options = await fetch() ?? [];
            return options
                .Take(PerModule)
                .Select(o => new SearchHit(o.Id, Trim(o.Label), key, byId ? o.Id.ToString() : Trim(o.Label)))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static Task<IReadOnlyList<SearchHit>> Paged<T>(
        string term,
        Func<AdminListRequest, Task<(IReadOnlyList<T> Rows, int TotalFiltered)>> fetch,
        Func<T, int> id,
        Func<T, string?> label,
        string key,
        Func<T, string>? route = null)
    {
        var request = AdminListRequest.From(term, null, null, null, null, 1, PerModule) with { IsDeleted = false };
        return Rows(() => fetch(request), id, label, key, route);
    }

    private static async Task<IReadOnlyList<SearchHit>> Rows<T>(
        Func<Task<(IReadOnlyList<T> Rows, int TotalFiltered)>> fetch,
        Func<T, int> id,
        Func<T, string?> label,
        string key,
        Func<T, string>? route = null)
    {
        try
        {
            var (rows, _) = await fetch();
            return (rows ?? [])
                .Take(PerModule)
                .Select(x => new SearchHit(id(x), Trim(label(x)), key, route is null ? Trim(label(x)) : route(x)))
                .Where(h => h.Label.Length > 0)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string Caption(string? altText, string? url)
    {
        if (!string.IsNullOrWhiteSpace(altText)) return altText.Trim();

        var file = (url ?? "").Trim().TrimEnd('/');
        var cut = file.LastIndexOfAny(['/', '\\']);
        if (cut >= 0) file = file[(cut + 1)..];
        return file.Length > 0 ? file : "görsel";
    }

    private static string Trim(string? text)
    {
        var value = (text ?? "").Trim();
        return value.Length <= LabelLength ? value : value[..(LabelLength - 1)] + "…";
    }
}
