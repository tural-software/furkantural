using System.Collections.Concurrent;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// Eşikler <c>Chat:RateLimit</c> altındaki MaxPerWindow (20) ve WindowSeconds (10) değerlerinden
/// okunur. Pencere kayandır: sabit dilimlere bölünmediği için dilim sınırında iki katı mesaj
/// gönderilemez.
/// </summary>
public sealed class MessageRateLimiter : IMessageRateLimiter
{
    private readonly int _max;
    private readonly TimeSpan _window;
    private readonly IClock _clock;
    private readonly ConcurrentDictionary<int, Queue<DateTime>> _hits = new();

    public MessageRateLimiter(IConfiguration configuration, IClock clock)
    {
        _max = configuration.GetValue<int?>("Chat:RateLimit:MaxPerWindow") ?? 20;
        var seconds = configuration.GetValue<int?>("Chat:RateLimit:WindowSeconds") ?? 10;
        _window = TimeSpan.FromSeconds(seconds <= 0 ? 10 : seconds);
        _clock = clock;
    }

    public bool TryRegisterSend(int userId)
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