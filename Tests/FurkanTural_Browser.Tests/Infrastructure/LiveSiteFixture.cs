using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Playwright;

namespace FurkanTural_Browser.Tests.Infrastructure;

public static class BrowserSweep
{
    public const string Collection = "browser-sweep";
}

[CollectionDefinition(BrowserSweep.Collection)]
public sealed class BrowserSweepCollection : ICollectionFixture<LiveSiteFixture>;

public sealed class LiveSiteFixture : IAsyncLifetime
{
    public const string ConsentSeed = "try { localStorage.setItem('ft.consent', '1'); } catch (e) { }";

    private static readonly string[] IgnoredOrigins =
    [
        "cloudflareinsights.com",
        "challenges.cloudflare.com"
    ];

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _probeScript = "";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, PageSnapshot> _snapshots = [];
    private readonly Dictionary<(Access, string), IBrowserContext> _contexts = [];
    private readonly Dictionary<Access, string?> _storageState = [];
    private readonly Dictionary<Access, string> _authFailure = [];
    private readonly Dictionary<string, string> _resolvedPaths = [];
    private readonly ConcurrentDictionary<string, Task<string?>> _appDown = [];

    public string? StartupFailure { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            var probePath = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "probe.js");
            _probeScript = await File.ReadAllTextAsync(probePath);

            _playwright = await Playwright.CreateAsync();
            _browser = await LaunchAsync(_playwright);
        }
        catch (Exception ex)
        {
            StartupFailure = ex.Message;
        }
    }

    private static async Task<IBrowser> LaunchAsync(IPlaywright playwright)
    {
        var channels = Environment.GetEnvironmentVariable("SWEEP_BROWSER_CHANNEL") is { Length: > 0 } forced
            ? new[] { forced }
            : ["msedge", "chrome"];

        Exception? last = null;
        foreach (var channel in channels)
        {
            try
            {
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Channel = channel,
                    Headless = Environment.GetEnvironmentVariable("SWEEP_HEADED") != "1"
                });
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        try
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Hiçbir tarayıcı başlatılamadı. Kurulu Edge/Chrome bulunamadıysa 'pwsh bin/Debug/net10.0/playwright.ps1 install chromium' çalıştırın. Son hata: {ex.Message}", last);
        }
    }

    public void RequireInfrastructure()
    {
        if (StartupFailure is not null)
            throw new SkipException($"Tarayıcı katmanı başlatılamadı: {StartupFailure}");
    }

    public async Task RequireAppAsync(SiteApp app)
    {
        RequireInfrastructure();

        var reason = await _appDown.GetOrAdd(app.Name, _ => ProbeAsync(app));
        if (reason is not null)
            throw new SkipException($"{app.Name} ({app.BaseUrl}) ayakta değil: {reason}");
    }

    private static async Task<string?> ProbeAsync(SiteApp app)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            (await http.GetAsync(app.BaseUrl, HttpCompletionOption.ResponseHeadersRead)).Dispose();
            return null;
        }
        catch (Exception ex)
        {
            return ex.GetBaseException().Message;
        }
    }

    public async Task<PageSnapshot> SnapshotAsync(SitePage page, Viewport viewport, string theme)
    {
        await RequireAppAsync(page.App);

        var key = $"{page.Id}|{viewport.Name}|{theme}";
        await _gate.WaitAsync();
        try
        {
            if (_snapshots.TryGetValue(key, out var cached)) return cached;

            var context = await GetContextAsync(page.Access, theme);
            var path = await ResolvePathAsync(page, context);

            PageSnapshot snapshot;
            try
            {
                snapshot = await CaptureAsync(context, page, path, viewport, theme);
            }
            catch (PlaywrightException ex) when (IsTransport(ex))
            {
                await Task.Delay(1500);
                snapshot = await CaptureAsync(context, page, path, viewport, theme);
            }

            _snapshots[key] = snapshot;
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> ResolvePathAsync(SitePage page, IBrowserContext context)
    {
        if (page.Discover is null) return page.Path;
        if (_resolvedPaths.TryGetValue(page.Id, out var known)) return known;

        var browsePage = await context.NewPageAsync();
        try
        {
            await browsePage.GotoAsync(page.App.BaseUrl + page.DiscoverFrom, new PageGotoOptions { Timeout = 30000 });
            var href = await browsePage.EvaluateAsync<string?>(
                "sel => { const a = document.querySelector(sel); return a ? a.getAttribute('href') : null; }", page.Discover);

            if (string.IsNullOrWhiteSpace(href))
                throw new InvalidOperationException(
                    $"{page.Id}: '{page.DiscoverFrom}' üzerinde '{page.Discover}' seçicisiyle bağlantı bulunamadı; içerik boş olabilir.");

            _resolvedPaths[page.Id] = href;
            return href;
        }
        finally
        {
            await browsePage.CloseAsync();
        }
    }

    private async Task<PageSnapshot> CaptureAsync(
        IBrowserContext context, SitePage page, string path, Viewport viewport, string theme)
    {
        var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : page.App.BaseUrl + path;
        var browserPage = await context.NewPageAsync();
        var consoleErrors = new List<string>();
        var failedRequests = new List<string>();

        browserPage.Console += (_, msg) =>
        {
            if (msg.Type == "error" && !Ignored(msg.Text)) consoleErrors.Add(msg.Text.Trim());
        };
        browserPage.PageError += (_, error) =>
        {
            if (!Ignored(error)) consoleErrors.Add("pageerror: " + error.Trim());
        };
        browserPage.Response += (_, response) =>
        {
            if (response.Status >= 400 && !Ignored(response.Url))
                failedRequests.Add($"{response.Status} {response.Url}");
        };
        browserPage.RequestFailed += (_, request) =>
        {
            if (!Ignored(request.Url)) failedRequests.Add($"failed {request.Url} ({request.Failure})");
        };

        try
        {
            await browserPage.SetViewportSizeAsync(viewport.Width, viewport.Height);
            var response = await browserPage.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            await browserPage.EvaluateAsync("() => document.fonts ? document.fonts.ready.then(() => true) : true");
            await browserPage.EvaluateAsync(
                "() => Promise.all(Array.from(document.images).filter(i => !i.complete)" +
                ".map(i => new Promise(done => { i.onload = i.onerror = done; })))");
            await browserPage.WaitForTimeoutAsync(150);

            var probe = await browserPage.EvaluateAsync<JsonElement>(_probeScript);

            return PageSnapshot.From(
                page, viewport, theme, browserPage.Url, response?.Status ?? 0, probe,
                consoleErrors.Distinct().ToArray(),
                failedRequests.Distinct().ToArray());
        }
        finally
        {
            await browserPage.CloseAsync();
        }
    }

    private static bool IsTransport(PlaywrightException ex) =>
        ex.Message.Contains("ERR_NETWORK_IO_SUSPENDED", StringComparison.Ordinal) ||
        ex.Message.Contains("ERR_CONNECTION_RESET", StringComparison.Ordinal) ||
        ex.Message.Contains("ERR_NETWORK_CHANGED", StringComparison.Ordinal) ||
        ex.Message.Contains("ERR_ABORTED", StringComparison.Ordinal);

    private static bool Ignored(string text) =>
        IgnoredOrigins.Any(o => text.Contains(o, StringComparison.OrdinalIgnoreCase));

    public async Task<T> WithFirstTimeVisitorAsync<T>(SiteApp app, string path, Func<IPage, Task<T>> visit)
    {
        await RequireAppAsync(app);

        await _gate.WaitAsync();
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ReducedMotion = ReducedMotion.Reduce,
            Locale = "tr-TR"
        });

        try
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(app.BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
            return await visit(page);
        }
        finally
        {
            await context.CloseAsync();
            _gate.Release();
        }
    }

    private async Task<IBrowserContext> GetContextAsync(Access access, string theme)
    {
        if (_contexts.TryGetValue((access, theme), out var existing)) return existing;

        var state = access == Access.Public ? null : await GetStorageStateAsync(access);

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ColorScheme = theme == Themes.Light ? ColorScheme.Light : ColorScheme.Dark,
            ReducedMotion = ReducedMotion.Reduce,
            StorageState = state,
            IgnoreHTTPSErrors = true,
            Locale = "tr-TR"
        });

        await context.AddInitScriptAsync(ConsentSeed);
        _contexts[(access, theme)] = context;
        return context;
    }

    private async Task<string> GetStorageStateAsync(Access access)
    {
        if (_authFailure.TryGetValue(access, out var failure)) throw new SkipException(failure);
        if (_storageState.TryGetValue(access, out var cached) && cached is not null) return cached;

        var (app, userKey, passKey) = access switch
        {
            Access.ChatUser => (SiteMap.Chat, "SWEEP_CHAT_USER", "SWEEP_CHAT_PASS"),
            Access.AdminUser => (SiteMap.Admin, "SWEEP_ADMIN_USER", "SWEEP_ADMIN_PASS"),
            _ => throw new ArgumentOutOfRangeException(nameof(access))
        };

        var user = Environment.GetEnvironmentVariable(userKey);
        var pass = Environment.GetEnvironmentVariable(passKey);

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            var reason = $"{app.Name} oturumlu sayfaları atlandı: {userKey} ve {passKey} ortam değişkenleri tanımlı değil.";
            _authFailure[access] = reason;
            throw new SkipException(reason);
        }

        try
        {
            var state = await SignInAsync(app, access, user!, pass!);
            _storageState[access] = state;
            return state;
        }
        catch (Exception ex)
        {
            var reason = $"{app.Name} oturumu açılamadı: {ex.GetBaseException().Message}";
            _authFailure[access] = reason;
            throw new SkipException(reason);
        }
    }

    private static async Task<string> DescribeGateAsync(IPage page) =>
        await page.EvaluateAsync<string>(
            """
            () => new Promise(resolve => {
              const hint = document.getElementById('turnstileHint');
              const token = document.getElementById('turnstileToken');
              const b = document.querySelector("form#loginForm button[type='submit']");
              const head = [
                'widget=' + (document.querySelector('.cf-turnstile') ? 'var' : 'yok'),
                'iframe=' + document.querySelectorAll('iframe').length,
                'api=' + (typeof window.turnstile),
                'token=' + (token ? token.value.length : -1) + ' karakter',
                'ipucu="' + (hint ? hint.textContent.trim() : '') + '"'
              ];
              if (!b) return resolve(head.concat('düğme=yok').join(', '));
              const box = r => [Math.round(r.x), Math.round(r.y), Math.round(r.width), Math.round(r.height)].join('/');
              const r1 = b.getBoundingClientRect();
              requestAnimationFrame(() => requestAnimationFrame(() => {
                const r2 = b.getBoundingClientRect();
                const running = document.getAnimations
                  ? document.getAnimations().filter(a => a.playState === 'running')
                      .map(a => a.animationName || 'transition').slice(0, 4)
                  : ['bilinmiyor'];
                const cx = r2.left + r2.width / 2, cy = r2.top + r2.height / 2;
                const hit = document.elementFromPoint(cx, cy);
                const name = el => !el ? 'yok' : el.tagName.toLowerCase() + (el.id ? '#' + el.id : '') +
                  (typeof el.className === 'string' && el.className ? '.' + el.className.trim().split(/\s+/)[0] : '');
                resolve(head.concat([
                  'görünür=' + (b.offsetParent !== null),
                  'etkin=' + !b.disabled,
                  'kutu=' + box(r1) + ' -> ' + box(r2),
                  'süren animasyon=' + JSON.stringify(running),
                  'kaydırma=' + getComputedStyle(document.documentElement).scrollBehavior,
                  'görünüm=' + innerWidth + 'x' + innerHeight + ' kaydırma-y=' + Math.round(scrollY),
                  'merkezdeki öge=' + name(hit) + (hit === b ? ' (düğmenin kendisi)' : '')
                ]).join(', '));
              }));
            })
            """);

    private static async Task WaitForSettledAsync(IPage page, string selector) =>
        await page.WaitForFunctionAsync(
            """
            sel => {
              const el = document.querySelector(sel);
              if (!el) return false;
              const r = el.getBoundingClientRect();
              const now = [Math.round(r.x), Math.round(r.y), Math.round(r.width), Math.round(r.height)].join('/');
              const prev = window.__ftSettle;
              if (!prev || prev.box !== now) { window.__ftSettle = { box: now, at: Date.now() }; return false; }
              return Date.now() - prev.at >= 400;
            }
            """,
            selector,
            new PageWaitForFunctionOptions { Timeout = 15000, PollingInterval = 100 });

    private static string FirstLine(Exception ex) =>
        string.Join(" / ", ex.Message
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Take(6));

    private static async Task<string> DescribeOutcomeAsync(IPage page) =>
        await page.EvaluateAsync<string>(
            """
            () => {
              const notes = Array.from(document.querySelectorAll('[class*=toast], [role=alert], .auth-errors, .field-error'))
                .map(n => (n.textContent || '').trim()).filter(t => t.length);
              return 'adres=' + location.pathname + ', mesajlar=' + JSON.stringify(notes.slice(0, 4));
            }
            """);

    private async Task<string> SignInAsync(SiteApp app, Access access, string user, string pass)
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ReducedMotion = ReducedMotion.Reduce,
            Locale = "tr-TR"
        });
        await context.AddInitScriptAsync(ConsentSeed);
        var page = await context.NewPageAsync();
        var loginPath = access == Access.ChatUser ? "/Account/Login" : "/";
        var step = "başlangıç";
        try
        {
            step = "giriş sayfası açılamadı";
            await page.GotoAsync(app.BaseUrl + loginPath, new PageGotoOptions { Timeout = 20000 });

            step = "kullanıcı adı alanı doldurulamadı";
            await page.FillAsync("input[name='Username']", user, new PageFillOptions { Timeout = 15000 });

            step = "parola alanı doldurulamadı";
            await page.FillAsync("input[name='Password']", pass, new PageFillOptions { Timeout = 15000 });

            var submit = page.Locator("form#loginForm button[type='submit']");

            step = "gönder düğmesi bulunamadı";
            await submit.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            if (await submit.IsDisabledAsync())
            {
                step = "gönder düğmesi açılmadı, robot doğrulaması tamamlanmamış olabilir";
                await page.WaitForFunctionAsync(
                    "() => { const b = document.querySelector(\"form#loginForm button[type='submit']\"); return b && !b.disabled; }",
                    null, new PageWaitForFunctionOptions { Timeout = 20000 });
            }

            step = "gönder düğmesi yerine oturmadı, sayfanın yerleşimi durmadan kayıyor olabilir";
            await WaitForSettledAsync(page, "form#loginForm button[type='submit']");

            step = "gönder düğmesine tıklanamadı";
            await submit.ClickAsync(new LocatorClickOptions { Timeout = 15000 });

            step = "gönderimden sonra yönlendirme gelmedi";
            await page.WaitForFunctionAsync(
                "path => !location.pathname.toLowerCase().startsWith(path)",
                loginPath == "/" ? "/auth/login" : loginPath.ToLowerInvariant(),
                new PageWaitForFunctionOptions { Timeout = 20000 });

            return await context.StorageStateAsync();
        }
        catch (Exception ex)
        {
            var gate = access == Access.ChatUser ? " | " + await DescribeGateAsync(page) : "";
            throw new InvalidOperationException(
                $"{step} ({FirstLine(ex)}) - {await DescribeOutcomeAsync(page)}{gate}");
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts.Values)
        {
            try { await context.CloseAsync(); } catch { }
        }

        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _gate.Dispose();
    }
}
