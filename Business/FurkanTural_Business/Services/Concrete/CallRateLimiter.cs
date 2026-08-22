using System.Collections.Concurrent;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// Eşikler <c>Calls:RateLimit</c> altındaki MaxPerWindow (5) ve WindowSeconds (60) değerlerinden
/// okunur. Pencere kayandır: sabit dilimlere bölünmediği için dilim sınırında iki katı deneme
/// yapılamaz.
/// </summary>
public sealed class CallRateLimiter : ICallRateLimiter
{
    private readonly int _max;
    private readonly TimeSpan _window;
    private readonly IClock _clock;
    private readonly ConcurrentDictionary<int, Queue<DateTime>> _hits = new();

    public CallRateLimiter(IConfiguration configuration, IClock clock)
    {
        _max = configuration.GetValue<int?>("Calls:RateLimit:MaxPerWindow") ?? 5;
        var seconds = configuration.GetValue<int?>("Calls:RateLimit:WindowSeconds") ?? 60;
        _window = TimeSpan.FromSeconds(seconds <= 0 ? 60 : seconds);
        _clock = clock;
    }

    public bool TryStartCall(int userId)
    {
        var now = _clock.UtcNow;
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