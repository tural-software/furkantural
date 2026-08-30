using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class ContrastSweepTests(LiveSiteFixture site)
{
    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPageBothThemes), MemberType = typeof(SweepData))]
    public async Task Metin_kontrasti_WCAG_AA_esigini_gecer(string pageId, string theme)
    {
        var snapshot = await site.SnapshotAsync(SweepData.Page(pageId), Viewport.Desktop, theme);

        snapshot.AppliedTheme.Should().Be(theme,
            $"{snapshot.Where} istenen temayı uygulamadı; ölçüm yanlış temada yapılmış olur");

        snapshot.LowContrast.Should().BeEmpty(
            $"{snapshot.Where} WCAG AA kontrast eşiğinin altında metin içeriyor " +
            $"(normal metin 4.5:1, büyük metin 3:1):" + snapshot.Report(snapshot.LowContrast));
    }
}
