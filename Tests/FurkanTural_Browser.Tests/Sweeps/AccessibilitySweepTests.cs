using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class AccessibilitySweepTests(LiveSiteFixture site)
{
    private Task<PageSnapshot> DesktopAsync(string pageId) =>
        site.SnapshotAsync(SweepData.Page(pageId), Viewport.Desktop, Themes.Dark);

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Gorsellerin_alt_metni_vardir(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.MissingAlt.Should().BeEmpty(
            $"{snapshot.Where} üzerinde alt niteliği hiç olmayan görsel var (süs görseli için alt=\"\" yazılır):" +
            snapshot.Report(snapshot.MissingAlt));
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Form_denetimlerinin_adi_vardir(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.Unlabelled.Should().BeEmpty(
            $"{snapshot.Where} üzerinde erişilebilir adı olmayan form denetimi var:" +
            snapshot.Report(snapshot.Unlabelled));
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Baglantilarin_okunabilir_adi_vardir(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.NamelessLinks.Should().BeEmpty(
            $"{snapshot.Where} üzerinde metni ve aria-label'ı olmayan bağlantı var; ekran okuyucu yalnızca adresi okur:" +
            snapshot.Report(snapshot.NamelessLinks));
    }
}
