using System.Collections.Concurrent;
using FurkanTural_Application.Services.Abstract;

namespace FurkanTural_API.Realtime;

/// <summary>Kullanıcı başına açık bağlantı kimlikleri bellekte tutulur, dolayısıyla aynı kullanıcının birden çok sekmesi ve cihazı ayrı ayrı sayılır. Connect yalnızca ilk bağlantıda, Disconnect yalnızca son bağlantı kapandığında true döner; çağıran "çevrimiçi oldu" ve "çevrimdışı oldu" olaylarını bu dönüş değerine bakarak üretir.<para>Sayım süreç belleğindedir: uygulama yeniden başlarsa herkes çevrimdışı görünür ve istemcilerin yeniden bağlanması beklenir.</para></summary>
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

        _connections.TryRemove(userId, out _);
        return true;
    }

    public bool IsOnline(int userId)
        => _connections.TryGetValue(userId, out var set) && set.Count > 0;
}
