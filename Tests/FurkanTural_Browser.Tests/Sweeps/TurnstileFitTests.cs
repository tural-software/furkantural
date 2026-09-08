using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class TurnstileFitTests(LiveSiteFixture site)
{
    private static readonly Viewport[] Narrow =
    [
        new("phone-320", 320, 800),
        new("phone-361", 361, 800),
        new("phone-401", 401, 800)
    ];

    private const string Script =
        """
        async () => {
          if (document.fonts) { try { await document.fonts.ready; } catch (e) { } }
          const GENISLIK = 300;
          const kotu = [];
          const hedefler = document.querySelectorAll('.cf-turnstile');
          for (let i = 0; i < hedefler.length; i++) {
            const w = hedefler[i];
            if (w.offsetWidth === 0) continue;
            const st = getComputedStyle(w);
            const yer = w.offsetWidth - parseFloat(st.paddingLeft) - parseFloat(st.paddingRight);
            let olcek = 1;
            if (st.transform && st.transform.startsWith('matrix(')) {
              const n = parseFloat(st.transform.slice(7).split(',')[0]);
              if (!Number.isNaN(n)) olcek = n;
            }
            const gereken = GENISLIK * olcek;
            if (gereken > yer + 0.5) {
              kotu.push('  ' + (i + 1) + '. widget: yer ' + Math.round(yer) + 'px, gereken ' +
                Math.round(gereken) + 'px (olcek ' + olcek.toFixed(2) + '), tasma ' +
                Math.round(gereken - yer) + 'px');
            }
          }
          return kotu;
        }
        """;

    public static TheoryData<string, string> HerAcikSayfaDarEkranda()
    {
        var data = new TheoryData<string, string>();
        foreach (var page in SiteMap.Pages.Where(p => p.Access == Access.Public))
            foreach (var viewport in Narrow)
                data.Add(page.Id, viewport.Name);
        return data;
    }

    [SkippableTheory]
    [MemberData(nameof(HerAcikSayfaDarEkranda))]
    public async Task Turnstile_kendi_kutusuna_sigar(string pageId, string viewportName)
    {
        var page = SweepData.Page(pageId);
        var viewport = Narrow.Single(v => v.Name == viewportName);

        var tasanlar = await site.WithPageAsync(page, viewport, p => p.EvaluateAsync<string[]>(Script));

        tasanlar.Should().BeEmpty(
            $"{page.Id} sayfasında {viewport.Name} genişliğinde Turnstile kutusundan taşıyor. Widget " +
            "300px genişliğinde çizilir ve bu ölçü Cloudflare'ın kararıdır; sığdırmak bizim işimizdir, " +
            "dar ekranda ölçeklenmesi gerekir. Bu denetim widget'ın kendisini beklemez — api.js süpürme " +
            "tarayıcısında çizilmiyor, dolayısıyla canlı iframe'e bakan bir kural bu kusuru hiç göremez:" +
            Environment.NewLine + string.Join(Environment.NewLine, tasanlar));
    }
}
