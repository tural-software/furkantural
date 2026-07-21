using System.Collections.Concurrent;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// In-memory kayan pencere ile login deneme sınırı (brute-force savunması). Singleton.
/// Eşikler: <c>Auth:LoginThrottle:MaxAttempts</c> (vars. 5),
/// <c>Auth:LoginThrottle:WindowSeconds</c> (vars. 300),
/// <c>Auth:LoginThrottle:LockoutSeconds</c> (vars. 300).
///
/// KİLİT ANAHTARI = kullanıcı adı + İSTEMCİ IP'si (yalnızca kullanıcı adı DEĞİL).
/// Sebep: salt kullanıcı adına kilitlemek, saldırganın bilinen bir hesabı (örn. admin) kasten
/// ve bedelsiz biçimde süresiz kilitlemesine izin verirdi — meşru kullanıcı dışarıda kalırdı
/// (hesap-kilidi DoS'u). IP ile kapsamlandırınca saldırganın kilidi yalnızca KENDİ IP'sini
/// bağlar; meşru kullanıcı başka bir IP'den sorunsuz girer.
///
/// Takas: çok sayıda IP'ye dağılmış bir saldırı bu katmanı aşabilir. Bu bilinçli — hacim
/// tabanlı savunma önde duran Cloudflare'in işidir; burada amaç tek kaynaklı parola
/// denemelerini ucuza kesmek ve meşru kullanıcıyı asla dışarıda bırakmamaktır.
/// (OWASP da salt hesap-kilidi yerine IP tabanlı kısıtlamayı önerir.)
///
/// Var olmayan kullanıcı adları için de sayaç tutulur — aksi halde "kilitlendi" yanıtı, hesabın
/// gerçekten var olduğunu ele veren bir yan kanal olurdu (kullanıcı adı sayımı).
///
/// Not: In-memory olduğundan sayaçlar app pool geri dönüşümünde sıfırlanır ve süreçler arası
/// paylaşılmaz. Tek sunuculu in-process kurulum için (Docker/ayrı servis kısıtı) yeterli;
/// kalıcı kilit gerekirse DB'ye taşınmalı (entity + migration gerektirir).
/// </summary>
public sealed class LoginThrottle : ILoginThrottle
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _window;
    private readonly TimeSpan _lockout;
    private readonly IClock _clock;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ConcurrentDictionary<string, Attempts> _attempts = new(StringComparer.OrdinalIgnoreCase);

    // Bayat girdileri toplamak için periyodik süpürme (bkz. PruneIfDue).
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

            // Kilit doldu: temiz sayfa aç ki kullanıcı yeniden tam hak ile başlasın.
            entry.LockedUntil = null;
            entry.Failures.Clear();
            expired = true;
        }

        // Kilidini doldurmuş girdiyi sözlükten de düşür — aksi halde anahtar sonsuza dek kalırdı.
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

    // Anahtar: "kullanıcı adı|IP". IP çözülemezse (HttpContext yoksa — örn. birim testi)
    // yalnızca kullanıcı adına düşülür; üretimde RealClientIpMiddleware gerçek IP'yi koyar.
    private bool TryBuildKey(string? username, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(username)) return false;

        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        key = string.IsNullOrEmpty(ip) ? username.Trim() : $"{username.Trim()}|{ip}";
        return true;
    }

    // Sözlükten yalnızca başarılı giriş (Reset) veya kilidin dolması ile girdi silinir. Kilide
    // hiç ulaşmamış başarısız denemeler kendiliğinden temizlenmezdi → yavaş bellek sızıntısı
    // (ve benzersiz kullanıcı adı üreten bir saldırıda hızlı şişme). Bu süpürme, penceresi
    // dolmuş ve kilitli olmayan girdileri periyodik olarak atar.
    //
    // Interlocked ile tek seferde tek süpürme: eşzamanlı isteklerde iş tekrarlanmasın.
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
                    // Kilitli girdiye dokunma; kilit süresi bilgisini kaybetmeyelim.
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
