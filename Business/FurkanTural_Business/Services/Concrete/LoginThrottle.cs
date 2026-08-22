using System.Collections.Concurrent;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// Eşikler <c>Auth:LoginThrottle</c> altındaki MaxAttempts (5), WindowSeconds (300) ve
/// LockoutSeconds (300) değerlerinden okunur; sıfır veya negatif verilen ayar yok sayılıp varsayılana
/// düşülür.
///
/// Anahtarın IP yarısı istek bağlamından gelir. Bağlam yoksa — arka plan çağrısı veya birim testi —
/// anahtar yalnızca kullanıcı adına iner ve sınır o çağrılar arasında paylaşılır.
///
/// Sözlükten girdi yalnızca başarılı girişte veya kilit dolduğunda silinir; kilide hiç ulaşmamış
/// denemeler kendiliğinden temizlenmezdi. Bu yüzden periyodik bir süpürme çalışır ve penceresi dolmuş
/// kilitsiz girdileri atar: her başarısız denemede yeni bir kullanıcı adı üreten saldırı aksi hâlde
/// sözlüğü sınırsız büyütürdü.
/// </summary>
public sealed class LoginThrottle : ILoginThrottle
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _window;
    private readonly TimeSpan _lockout;
    private readonly IClock _clock;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ConcurrentDictionary<string, Attempts> _attempts = new(StringComparer.OrdinalIgnoreCase);

    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);
    private DateTime _lastSweep;
    private int _sweeping;

    private sealed class Attempts
    {
        public readonly Queue<DateTime> Failures = new();
        public DateTime? LockedUntil;
    }

    public LoginThrottle(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IClock clock)
    {
        _maxAttempts = configuration.GetValue<int?>("Auth:LoginThrottle:MaxAttempts") ?? 5;
        if (_maxAttempts <= 0) _maxAttempts = 5;

        var windowSeconds = configuration.GetValue<int?>("Auth:LoginThrottle:WindowSeconds") ?? 300;
        _window = TimeSpan.FromSeconds(windowSeconds <= 0 ? 300 : windowSeconds);

        var lockoutSeconds = configuration.GetValue<int?>("Auth:LoginThrottle:LockoutSeconds") ?? 300;
        _lockout = TimeSpan.FromSeconds(lockoutSeconds <= 0 ? 300 : lockoutSeconds);

        _httpContextAccessor = httpContextAccessor;
        _clock = clock;
        _lastSweep = clock.UtcNow;
    }

    public TimeSpan? GetRemainingLockout(string? username)
    {
        if (!TryBuildKey(username, out var key)) return null;
        if (!_attempts.TryGetValue(key, out var entry)) return null;

        bool expired;
        lock (entry)
        {
            if (entry.LockedUntil is not { } until) return null;

            var remaining = until - _clock.UtcNow;
            if (remaining > TimeSpan.Zero)
                return remaining;

            entry.LockedUntil = null;
            entry.Failures.Clear();
            expired = true;
        }

        if (expired)
            _attempts.TryRemove(key, out _);

        return null;
    }

    public void RegisterFailure(string? username)
    {
        if (!TryBuildKey(username, out var key)) return;

        var now = _clock.UtcNow;
        var entry = _attempts.GetOrAdd(key, _ => new Attempts());
        lock (entry)
        {
            while (entry.Failures.Count > 0 && now - entry.Failures.Peek() > _window)
                entry.Failures.Dequeue();

            entry.Failures.Enqueue(now);

            if (entry.Failures.Count >= _maxAttempts)
                entry.LockedUntil = now + _lockout;
        }

        PruneIfDue(now);
    }

    public void Reset(string? username)
    {
        if (!TryBuildKey(username, out var key)) return;
        _attempts.TryRemove(key, out _);
    }

    private bool TryBuildKey(string? username, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(username)) return false;

        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        key = string.IsNullOrEmpty(ip) ? username.Trim() : $"{username.Trim()}|{ip}";
        return true;
    }

    private void PruneIfDue(DateTime now)
    {
        if (now - _lastSweep < SweepInterval) return;
        if (Interlocked.Exchange(ref _sweeping, 1) == 1) return;

        try
        {
            _lastSweep = now;

            foreach (var pair in _attempts)
            {
                var entry = pair.Value;
                bool stale;
                lock (entry)
                {
                    if (entry.LockedUntil is { } until && until > now)
                        continue;

                    while (entry.Failures.Count > 0 && now - entry.Failures.Peek() > _window)
                        entry.Failures.Dequeue();

                    stale = entry.Failures.Count == 0;
                }

                if (stale)
                    _attempts.TryRemove(pair.Key, out _);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _sweeping, 0);
        }
    }
}