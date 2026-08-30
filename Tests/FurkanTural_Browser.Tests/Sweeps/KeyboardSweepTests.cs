using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class KeyboardSweepTests(LiveSiteFixture site)
{
    private static string Walk(IReadOnlyList<TabStop> stops) =>
        stops.Count == 0 ? " (hiç durak yok)"
            : Environment.NewLine + string.Join(Environment.NewLine, stops.Select(s => "  " + s));

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Her_klavye_duraginda_odak_gorunur(string pageId)
    {
        var page = SweepData.Page(pageId);
        var stops = await site.TabWalkAsync(page);

        Skip.If(stops.Count == 0, $"{page.Id}: klavyeyle ulaşılan öge yok");

        var invisible = stops.Where(s => !s.FocusRingVisible && !s.ThirdParty).ToArray();

        invisible.Should().BeEmpty(
            $"{page.Id} üzerinde odaklanınca hiçbir şeyi değişmeyen durak var; klavyeyle gezen " +
            "kullanıcı nerede olduğunu göremez (WCAG 2.4.7):" + Walk(invisible));
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Odaklanan_oge_ekranda_kalir(string pageId)
    {
        var page = SweepData.Page(pageId);
        var stops = await site.TabWalkAsync(page);

        Skip.If(stops.Count == 0, $"{page.Id}: klavyeyle ulaşılan öge yok");

        var offscreen = stops.Where(s => !s.InViewport && !s.ThirdParty).ToArray();

        offscreen.Should().BeEmpty(
            $"{page.Id} üzerinde odaklandığı hâlde görünüm dışında kalan öge var; odak halkası " +
            "ekranda olmayan bir yere gider:" + Walk(offscreen));
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Klavye_tuzagi_yok(string pageId)
    {
        var page = SweepData.Page(pageId);
        var stops = await site.TabWalkAsync(page);

        Skip.If(stops.Count < 3, $"{page.Id}: tuzak değerlendirmek için yeterli durak yok");

        var distinct = stops.Select(s => s.Element + "|" + s.Text).Distinct().Count();

        distinct.Should().BeGreaterThan(2,
            $"{page.Id} üzerinde Tab {stops.Count} adımda yalnızca {distinct} ayrı ögeye uğradı; " +
            "odak bir yere kilitlenmiş olabilir:" + Walk(stops));
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Pozitif_tabindex_kullanilmaz(string pageId)
    {
        var snapshot = await site.SnapshotAsync(SweepData.Page(pageId), Viewport.Desktop, Themes.Dark);

        snapshot.PositiveTabindex.Should().BeEmpty(
            $"{snapshot.Where} pozitif tabindex kullanıyor; bu, odak sırasını belgenin sırasından " +
            "koparır ve sayfaya yeni bir öge eklendiğinde sıra sessizce bozulur:" +
            snapshot.Report(snapshot.PositiveTabindex));
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Tekrar_eden_gezinme_atlanabilir(string pageId)
    {
        var page = SweepData.Page(pageId);
        var snapshot = await site.SnapshotAsync(page, Viewport.Desktop, Themes.Dark);

        Skip.If(snapshot.NavLandmarks == 0, $"{page.Id}: atlanacak gezinme bloğu yok");
        Skip.If(snapshot.AutofocusPresent,
            $"{page.Id}: sayfa bir alana autofocus veriyor, ilk Tab zaten formun içinden devam eder");

        var stops = await site.TabWalkAsync(page);
        Skip.If(stops.Count == 0, $"{page.Id}: klavyeyle ulaşılan öge yok");

        var first = stops[0];

        first.Href.Should().StartWith("#",
            $"{page.Id} her sayfada aynı gezinmeyi tekrarlıyor ama ilk Tab durağı içeriğe atlama " +
            $"bağlantısı değil ({first}). Klavye kullanıcısı her sayfada gezinmenin tamamını " +
            "geçmek zorunda kalır (WCAG 2.4.1).");
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Sayfanin_ana_bolgesi_isaretli(string pageId)
    {
        var snapshot = await site.SnapshotAsync(SweepData.Page(pageId), Viewport.Desktop, Themes.Dark);

        snapshot.MainLandmarks.Should().Be(1,
            $"{snapshot.Where} sayfasında {snapshot.MainLandmarks} adet main bölgesi var; " +
            "ekran okuyucu kullanıcısı doğrudan içeriğe bu işaretle atlar");
    }
}
