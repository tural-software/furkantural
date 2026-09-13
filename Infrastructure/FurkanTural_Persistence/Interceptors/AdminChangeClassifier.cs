using FurkanTural_Application.DTOs.Common;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FurkanTural_Persistence.Interceptors;

public static class AdminChangeClassifier
{
    public static readonly IReadOnlyDictionary<Type, string> Watched = new Dictionary<Type, string>
    {
        [typeof(Comment)] = AdminListKinds.Comment,
        [typeof(Contact)] = AdminListKinds.Contact,
        [typeof(Report)] = AdminListKinds.Report,
        [typeof(User)] = AdminListKinds.User,
        [typeof(UserFriend)] = AdminListKinds.Friend,
        [typeof(ChatMessage)] = AdminListKinds.Message,
        [typeof(CallLog)] = AdminListKinds.Call,
        [typeof(Subscriber)] = AdminListKinds.Subscriber,
        [typeof(NewsletterIssue)] = AdminListKinds.Newsletter
    };

    public static IReadOnlyList<AdminEntityChange> Classify(IEnumerable<EntityEntry> entries)
    {
        var counts = new Dictionary<string, (int Added, int Changed)>();

        foreach (var entry in entries)
        {
            if (!Watched.TryGetValue(entry.Metadata.ClrType, out var kind))
                continue;

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            counts.TryGetValue(kind, out var current);
            counts[kind] = entry.State == EntityState.Added
                ? (current.Added + 1, current.Changed)
                : (current.Added, current.Changed + 1);
        }

        return [.. counts.Select(x => new AdminEntityChange(x.Key, x.Value.Added, x.Value.Changed))];
    }
}
