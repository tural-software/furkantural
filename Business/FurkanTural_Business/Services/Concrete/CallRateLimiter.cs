using System.Collections.Concurrent;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// In-memory kayan pencere ile arama-başlatma hız sınırı. Singleton.
/// Eşikler: <c>Calls:RateLimit:MaxPerWindow</c> (vars. 5), <c>Calls:RateLimit:WindowSeconds</c> (vars. 60).
/// </summary>
public sealed class CallRateLimiter : ICallRateLimiter
{
    private readonly int _max;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<int, Queue<DateTime>> _hits = new();

    public CallRateLimiter(IConfiguration configuration)
    {
        _max = configuration.GetValue<int?>("Calls:RateLimit:MaxPerWindow") ?? 5;
        var seconds = configuration.GetValue<int?>("Calls:RateLimit:WindowSeconds") ?? 60;
        _window = TimeSpan.FromSeconds(seconds <= 0 ? 60 : seconds);
    }

    public bool TryStartCall(int userId)
    {
        var now = DateTime.UtcNow;
        var queue = _hits.GetOrAdd(userId, _ => new Queue<DateTime>());
        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > _window)
                queue.Dequeue();

            if (queue.Count >= _max)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }
}
