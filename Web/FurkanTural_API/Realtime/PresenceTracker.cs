using System.Collections.Concurrent;
using FurkanTural_Application.Services.Abstract;

namespace FurkanTural_API.Realtime;

/// <summary>
/// <see cref="IPresenceTracker"/>'ın bellek içi uygulaması. Kullanıcı başına aktif
/// bağlantı kimliklerini tutar; aynı kullanıcının birden çok sekmesi/cihazı güvenle sayılır.
/// </summary>
public class PresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();

    public bool Connect(int userId, string connectionId)
    {
        var set = _connections.GetOrAdd(userId, static _ => new HashSet<string>());
        lock (set)
        {
            var wasEmpty = set.Count == 0;
            set.Add(connectionId);
            return wasEmpty;
        }
    }

    public bool Disconnect(int userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var set))
            return false;

        lock (set)
        {
            set.Remove(connectionId);
            if (set.Count > 0)
                return false;
        }

        // Boş kalan girdiyi temizle (yarış durumunda yeniden eklenmişse koru).
        _connections.TryRemove(userId, out _);
        return true;
    }

    public bool IsOnline(int userId)
        => _connections.TryGetValue(userId, out var set) && set.Count > 0;
}
