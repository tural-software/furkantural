using FurkanTural_Application.DTOs.Common;

namespace FurkanTural_API.Realtime;

public sealed class AdminChangeWindows
{
    private sealed class Window(DateTime dueAt)
    {
        public DateTime DueAt { get; } = dueAt;
        public Dictionary<int, (int Added, int Changed)> ByActor { get; } = [];
    }

    private readonly Dictionary<string, Window> _open = [];

    public static TimeSpan WindowFor(string kind) => kind switch
    {
        AdminListKinds.Comment or AdminListKinds.Contact or AdminListKinds.Report => TimeSpan.FromSeconds(1),
        AdminListKinds.User or AdminListKinds.Friend or AdminListKinds.Subscriber => TimeSpan.FromSeconds(2),
        AdminListKinds.Newsletter => TimeSpan.FromSeconds(5),
        AdminListKinds.Message or AdminListKinds.Call => TimeSpan.FromSeconds(10),
        _ => TimeSpan.FromSeconds(2)
    };

    public DateTime? NextDueAt => _open.Count == 0 ? null : _open.Values.Min(w => w.DueAt);

    public void Add(string kind, int actorId, int added, int changed, DateTime now)
    {
        if (!_open.TryGetValue(kind, out var window))
        {
            window = new Window(now + WindowFor(kind));
            _open[kind] = window;
        }

        window.ByActor.TryGetValue(actorId, out var current);
        window.ByActor[actorId] = (current.Added + added, current.Changed + changed);
    }

    public IReadOnlyList<AdminListChangeDto> TakeDue(DateTime now)
    {
        var dueKinds = _open.Where(x => x.Value.DueAt <= now).Select(x => x.Key).ToList();
        var due = new List<AdminListChangeDto>();

        foreach (var kind in dueKinds)
        {
            foreach (var (actorId, counts) in _open[kind].ByActor)
                due.Add(new AdminListChangeDto(kind, actorId, counts.Added, counts.Changed));

            _open.Remove(kind);
        }

        return due;
    }
}
