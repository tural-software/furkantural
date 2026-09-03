using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class LayoutBaselineTests(LiveSiteFixture site)
{
    private static readonly string[] Watched =
    [
        "Chat/",
        "Chat/Home/Privacy",
        "Chat/Home/Agreement",
        "Chat/offline.html"
    ];

    public static TheoryData<string, string> WatchedPages()
    {
        var data = new TheoryData<string, string>();
        foreach (var id in Watched)
            foreach (var viewport in Viewport.All)
                data.Add(id, viewport.Name);
        return data;
    }

    private static string[] Lines(string text) =>
        text.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToArray();

    private static string BaselineDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Baselines");
            if (Directory.Exists(candidate)) return candidate;
            if (File.Exists(Path.Combine(directory.FullName, "FurkanTural_Browser.Tests.csproj")))
            {
                Directory.CreateDirectory(candidate);
                return candidate;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Baselines dizini bulunamadı");
    }

    [SkippableTheory]
    [MemberData(nameof(WatchedPages))]
    public async Task Yerlesim_onayli_temelden_sapmaz(string pageId, string viewportName)
    {
        var page = SweepData.Page(pageId);
        var viewport = SweepData.Screen(viewportName);
        var file = Path.Combine(BaselineDirectory(),
            $"{pageId.Replace('/', '_').Replace(".html", "")}-{viewport.Name}.txt");

        var current = await site.FingerprintAsync(page, viewport);

        if (Environment.GetEnvironmentVariable("SWEEP_UPDATE_BASELINES") == "1" || !File.Exists(file))
        {
            await File.WriteAllTextAsync(file, current);
            Skip.If(true, $"{Path.GetFileName(file)} yazıldı; bir sonraki koşuda karşılaştırılacak");
        }

        var baseline = await File.ReadAllTextAsync(file);

        var expected = Lines(baseline);
        var actual = Lines(current);
        if (expected.SequenceEqual(actual)) return;

        var added = actual.Except(expected).Take(12).ToArray();
        var removed = expected.Except(actual).Take(12).ToArray();

        var report = string.Join(Environment.NewLine,
            removed.Select(l => "  - " + l).Concat(added.Select(l => "  + " + l)));

        actual.Should().BeEquivalentTo(expected,
            $"{page.Id} @ {viewport.Name} yerleşimi onaylı temelden sapıyor. Değişiklik kasıtlıysa " +
            $"SWEEP_UPDATE_BASELINES=1 ile temeli yenileyin ve farkı gözden geçirin:" +
            Environment.NewLine + report);
    }
}
