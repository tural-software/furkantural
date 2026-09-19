using System.Collections.Concurrent;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

public sealed class AbuseThrottle : IAbuseThrottle
{
    private static readonly Dictionary<string, (int Max, int Seconds)> Defaults = new(StringComparer.Ordinal)
    {
        [AbuseBuckets.Contact] = (3, 600),
        [AbuseBuckets.Newsletter] = (5, 600),
        [AbuseBuckets.Report] = (10, 3600),
        [AbuseBuckets.FriendRequest] = (20, 3600),
        [AbuseBuckets.CallConfig] = (30, 3600)
    };

    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;
    private readonly IClock _clock;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _hits = new(StringComparer.Ordinal);

    private DateTime _lastSweep;
    private int _sweeping;

    public AbuseThrottle(IConfiguration configuration, IClock clock)
    {
        _configuration = configuration;
        _clock = clock;
        _lastSweep = clock.UtcNow;
    }

    public bool TryRegister(string bucket, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true;

        var (max, window) = Limits(bucket);
        var now = _clock.UtcNow;
        var queue = _hits.GetOrAdd($"{bucket}|{key.Trim()}", _ => new Queue<DateTime>());

        bool allowed;
        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > window)
                queue.Dequeue();

            allowed = queue.Count < max;
            if (allowed)
                queue.Enqueue(now);
        }

        Prune(now);
        return allowed;
    }

    private (int Max, TimeSpan Window) Limits(string bucket)
    {
        var fallback = Defaults.TryGetValue(bucket, out var value) ? value : (Max: 5, Seconds: 300);

        var max = _configuration.GetValue<int?>($"Abuse:{bucket}:MaxPerWindow") ?? fallback.Max;
        if (max <= 0) max = fallback.Max;

        var seconds = _configuration.GetValue<int?>($"Abuse:{bucket}:WindowSeconds") ?? fallback.Seconds;
        if (seconds <= 0) seconds = fallback.Seconds;

        return (max, TimeSpan.FromSeconds(seconds));
    }

    private void Prune(DateTime now)
    {
        if (now - _lastSweep < SweepInterval) return;
        if (Interlocked.Exchange(ref _sweeping, 1) == 1) return;

        try
        {
            _lastSweep = now;

            foreach (var pair in _hits)
            {
                var queue = pair.Value;
                bool stale;
                lock (queue)
                {
                    while (queue.Count > 0 && now - queue.Peek() > TimeSpan.FromHours(1))
                        queue.Dequeue();

                    stale = queue.Count == 0;
                }

                if (stale)
                    _hits.TryRemove(pair.Key, out _);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _sweeping, 0);
        }
    }
}
