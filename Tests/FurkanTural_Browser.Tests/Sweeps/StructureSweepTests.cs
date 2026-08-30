using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class StructureSweepTests(LiveSiteFixture site)
{
    private Task<PageSnapshot> DesktopAsync(string pageId) =>
        site.SnapshotAsync(SweepData.Page(pageId), Viewport.Desktop, Themes.Dark);

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Sayfanin_tam_bir_h1_basligi_vardir(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.H1Count.Should().Be(1,
            $"{snapshot.Where} sayfasında {snapshot.H1Count} adet h1 var; her sayfanın tek bir üst düzey başlığı olmalı. " +
            $"Başlıklar:{snapshot.Report(snapshot.Headings.Select(h => $"h{h.Level}: {h.Text}").ToArray())}");
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Baslik_seviyeleri_atlanmaz(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        var jumps = new List<string>();
        var previous = 0;
        foreach (var heading in snapshot.Headings)
        {
            if (previous != 0 && heading.Level > previous + 1)
                jumps.Add($"h{previous} -> h{heading.Level}: {heading.Text}");
            previous = heading.Level;
        }

        jumps.Should().BeEmpty($"{snapshot.Where} başlık hiyerarşisinde seviye atlanmış:{snapshot.Report(jumps)}");
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Sayfanin_dili_ve_basligi_bildirilir(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.Lang.Should().NotBeEmpty($"{snapshot.Where} html etiketinde lang yok; ekran okuyucu telaffuzu buna bağlı");
        snapshot.Title.Should().NotBeEmpty($"{snapshot.Where} sayfa başlığı boş");
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Ayni_id_iki_kez_kullanilmaz(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.DuplicateIds.Should().BeEmpty(
            $"{snapshot.Where} aynı id'yi birden çok ögede kullanıyor; label eşleşmesi ve script seçicileri yanlış ögeyi bulur:" +
            snapshot.Report(snapshot.DuplicateIds));
    }
}
