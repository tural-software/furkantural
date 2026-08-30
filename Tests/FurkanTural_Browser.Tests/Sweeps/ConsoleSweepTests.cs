using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class ConsoleSweepTests(LiveSiteFixture site)
{
    private Task<PageSnapshot> DesktopAsync(string pageId) =>
        site.SnapshotAsync(SweepData.Page(pageId), Viewport.Desktop, Themes.Dark);

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Sayfa_konsola_hata_yazmaz(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.ConsoleErrors.Should().BeEmpty(
            $"{snapshot.Where} konsola hata yazıyor:" + snapshot.Report(snapshot.ConsoleErrors));
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Sayfanin_hicbir_kaynagi_basarisiz_olmaz(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.FailedRequests.Should().BeEmpty(
            $"{snapshot.Where} yüklenemeyen kaynak istiyor:" + snapshot.Report(snapshot.FailedRequests));
    }
}
