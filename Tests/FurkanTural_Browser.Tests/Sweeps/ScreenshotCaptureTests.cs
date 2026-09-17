using FurkanTural_Browser.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FurkanTural_Browser.Tests.Sweeps;

/// <summary>Sayfaların tanıtım görüntülerini üretir. İddia taşımaz: `SWEEP_SCREENSHOT_DIR` verilmediğinde atlanır, verildiğinde her sayfayı kendi dosyasına yazar.</summary>
[Collection(BrowserSweep.Collection)]
public sealed class ScreenshotCaptureTests(LiveSiteFixture site)
{
    private const string SettleScript =
        """
        () => new Promise(resolve => {
          const sheet = new CSSStyleSheet();
          sheet.replaceSync('*, *::before, *::after { transition: none !important; animation: none !important; scroll-behavior: auto !important; }');
          document.adoptedStyleSheets = [...document.adoptedStyleSheets, sheet];
          let y = 0;
          const step = () => {
            y += innerHeight;
            scrollTo(0, y);
            if (y < document.documentElement.scrollHeight) return setTimeout(step, 120);
            scrollTo(0, 0);
            setTimeout(resolve, 400);
          };
          step();
        })
        """;

    private static string FileName(string pageId, Viewport viewport, string theme)
    {
        var slug = pageId.Trim('/');
        foreach (var bad in Path.GetInvalidFileNameChars().Concat(['/', '?', '=', '&']))
            slug = slug.Replace(bad, '-');
        return $"{slug.Trim('-')}-{viewport.Name}-{theme}.png";
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Sayfanin_ekran_goruntusu_alinir(string pageId)
    {
        var directory = Environment.GetEnvironmentVariable("SWEEP_SCREENSHOT_DIR");
        Skip.If(string.IsNullOrWhiteSpace(directory),
            "SWEEP_SCREENSHOT_DIR tanımlı değil; görüntü yakalama yalnızca istendiğinde çalışır.");

        var viewport = SweepData.Screen(Environment.GetEnvironmentVariable("SWEEP_SCREENSHOT_VIEWPORT") ?? Viewport.Desktop.Name);
        var theme = Environment.GetEnvironmentVariable("SWEEP_SCREENSHOT_THEME") ?? Themes.Dark;

        Directory.CreateDirectory(directory!);
        var path = Path.Combine(directory!, FileName(pageId, viewport, theme));

        await site.WithPageAsync(SweepData.Page(pageId), viewport, theme, async browserPage =>
        {
            await browserPage.EvaluateAsync(SettleScript);
            await browserPage.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            return true;
        });
    }
}
