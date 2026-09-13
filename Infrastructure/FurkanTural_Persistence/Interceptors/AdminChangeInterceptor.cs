using System.Runtime.CompilerServices;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Services.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FurkanTural_Persistence.Interceptors;

public sealed class AdminChangeInterceptor(
    IEnumerable<IAdminChangeFeed> feeds,
    ILogger<AdminChangeInterceptor> logger) : SaveChangesInterceptor
{
    private readonly IAdminChangeFeed[] _feeds = [.. feeds];
    private readonly ILogger<AdminChangeInterceptor> _logger = logger;
    private readonly ConditionalWeakTable<DbContext, IReadOnlyList<AdminEntityChange>> _pending = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Publish(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Publish(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Discard(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override void SaveChangesCanceled(DbContextEventData eventData)
    {
        Discard(eventData.Context);
        base.SaveChangesCanceled(eventData);
    }

    public override Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);
        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null || _feeds.Length == 0)
            return;

        var changes = AdminChangeClassifier.Classify(context.ChangeTracker.Entries());

        if (changes.Count == 0)
            _pending.Remove(context);
        else
            _pending.AddOrUpdate(context, changes);
    }

    private void Publish(DbContext? context)
    {
        if (context is null || !_pending.TryGetValue(context, out var changes))
            return;

        _pending.Remove(context);

        foreach (var feed in _feeds)
        {
            try
            {
                feed.Publish(changes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yönetici değişiklik yayını başarısız oldu.");
            }
        }
    }

    private void Discard(DbContext? context)
    {
        if (context is not null)
            _pending.Remove(context);
    }
}
