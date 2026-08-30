using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class NavigationSweepTests(LiveSiteFixture site)
{
    private Task<PageSnapshot> DesktopAsync(string pageId) =>
        site.SnapshotAsync(SweepData.Page(pageId), Viewport.Desktop, Themes.Dark);

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Sayfa_hatasiz_yanit_doner(string pageId)
    {
        var snapshot = await DesktopAsync(pageId);

        snapshot.Status.Should().BeLessThan(400,
            $"{snapshot.Where} ana belge {snapshot.Status} döndürdü");
    }

    [SkippableTheory]
    [MemberData(nameof(SweepData.EveryPage), MemberType = typeof(SweepData))]
    public async Task Oturumlu_sayfa_giris_ekranina_dusmez(string pageId)
    {
        var page = SweepData.Page(pageId);
        Skip.If(page.Access == Access.Public, "genel sayfa; oturum gerektirmiyor");

        var snapshot = await DesktopAsync(pageId);

        snapshot.LoginFormPresent.Should().BeFalse(
            $"{snapshot.Where} giriş formunu gösteriyor ({snapshot.Url}); oturum taşınmamış demektir " +
            "ve tarama o sayfayı değil giriş sayfasını ölçüyor olur. Adres denetimi yetmez: " +
            "Admin'in giriş ekranı kök adreste durduğu için adreste 'login' geçmez");
    }
}
