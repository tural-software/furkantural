using Microsoft.AspNetCore.Http;

namespace FurkanTural_Admin.Tests.Infrastructure;

/// <summary>
/// ISession'ın in-memory Dictionary tabanlı stub implementasyonu.
/// Gerçek ASP.NET Core session middleware gerektirmeden controller testlerinde kullanılır.
/// </summary>
public sealed class MockSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public string Id => "test-session-id";
    public bool IsAvailable => true;

    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _store.Remove(key);

    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_store.TryGetValue(key, out var v))
        {
            value = v;
            return true;
        }
        value = [];
        return false;
    }
}
