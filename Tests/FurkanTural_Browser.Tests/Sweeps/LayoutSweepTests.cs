using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class LayoutSweepTests(LiveSiteFixture site)
{
    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPageEveryWidth), MemberType = typeof(SweepData))]
    public async Task Sayfa_yatay_kaydirma_uretmez(string pageId, string viewportName)
    {
        var snapshot = await site.SnapshotAsync(SweepData.Page(pageId), SweepData.Screen(viewportName), Themes.Dark);

        snapshot.Overflow.Should().BeLessThanOrEqualTo(0,
            $"{snapshot.Where} yatay kaydırma çubuğu üretiyor ({snapshot.ScrollWidth}px içerik, {snapshot.ClientWidth}px görünüm). " +
            $"Taşıran ögeler:{snapshot.Report(snapshot.Overflowers)}");
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPageEveryWidth), MemberType = typeof(SweepData))]
    public async Task Hicbir_kapsayici_yanlamasina_tasmaz(string pageId, string viewportName)
    {
        var snapshot = await site.SnapshotAsync(SweepData.Page(pageId), SweepData.Screen(viewportName), Themes.Dark);

        snapshot.Scrollers.Should().BeEmpty(
            $"{snapshot.Where} içinde yanlamasına taşan kapsayıcı var. Belge düzeyinde kaydırma çubuğu " +
            "görünmese de içerik ya kayıyor ya da kırpılıyor; bilerek yatay kayan kutular probe.js " +
            "içindeki ALLOWED_SCROLLERS listesinde tanımlıdır:" + snapshot.Report(snapshot.Scrollers));
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPageEveryWidth), MemberType = typeof(SweepData))]
    public async Task Dokunma_hedefleri_yeterince_buyuk(string pageId, string viewportName)
    {
        var snapshot = await site.SnapshotAsync(SweepData.Page(pageId), SweepData.Screen(viewportName), Themes.Dark);

        snapshot.SmallTargets.Should().BeEmpty(
            $"{snapshot.Where} üzerinde 24x24'ten küçük dokunma hedefi var (WCAG 2.2 - 2.5.8):" +
            snapshot.Report(snapshot.SmallTargets));
    }
}
