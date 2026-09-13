using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class ConsentGateTests(LiveSiteFixture site)
{
    [SkippableFact]
    public async Task Ilk_ziyarette_cerez_bilgilendirmesi_gorunur()
    {
        var shown = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/", async page =>
            await page.Locator("#consentNotice").IsVisibleAsync());

        shown.Should().BeTrue(
            "bilgilendirme ilk ziyarette görünmezse ziyaretçi hangi çerezlerin kullanıldığını hiç duymamış olur");
    }

    [SkippableFact]
    public async Task Tamam_denince_kapanir_ve_bir_daha_gosterilmez()
    {
        var stillShown = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/", async page =>
        {
            await page.ClickAsync("#consentOk");
            await page.Locator("#consentNotice").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });

            await page.GotoAsync(SiteMap.Chat.BaseUrl + "/Account/Login",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });

            return await page.Locator("#consentNotice").IsVisibleAsync();
        });

        stillShown.Should().BeFalse(
            "kapatılan bilgilendirme her sayfada yeniden çıkarsa formların üstünde durmaya devam eder");
    }

    [SkippableFact]
    public async Task Bilgilendirme_giris_formunu_ortmez()
    {
        var covered = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/Account/Login", async page =>
            await page.EvaluateAsync<bool>(
                """
                () => {
                  const field = document.querySelector("form#loginForm input[name='Username']");
                  if (!field) return true;
                  const r = field.getBoundingClientRect();
                  const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
                  return !hit || !!hit.closest('#consentNotice');
                }
                """));

        covered.Should().BeFalse(
            "yalnızca zorunlu çerez kullanıldığı için onay beklenmez; bilgilendirme sayfayı kilitlerse " +
            "ziyaretçiden gereksiz bir onay koparılmış olur");
    }

    [SkippableFact]
    public async Task Kapatma_cerezi_yazilir_ve_bilgilendirme_sunucudan_hic_gelmez()
    {
        var (cookieWritten, stillRendered) = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/", async page =>
        {
            await page.ClickAsync("#consentOk");
            var cookies = await page.EvaluateAsync<string>("() => document.cookie");

            await page.GotoAsync(SiteMap.Chat.BaseUrl + "/Account/Login",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });

            return (cookies.Contains("ft.consent=1"), await page.Locator("#consentNotice").CountAsync());
        });

        cookieWritten.Should().BeTrue(
            "kapatma yalnızca localStorage'da tutulursa sunucu onu göremez; bilgilendirmeyi her sayfada yeniden " +
            "basar ve localStorage yazılamayan bir tarayıcıda kapatma hiç yapışmaz");
        stillRendered.Should().Be(0,
            "kapatıldıktan sonra bilgilendirme HTML'e hiç girmemeli; girerse görünürlüğü yine JS zamanlamasına " +
            "kalır ve sayfa açılışında bir görünüp kaybolur");
    }

    [SkippableFact]
    public async Task Bilgilendirme_hicbir_betik_calismadan_ekranda()
    {
        var visible = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/Account/Login", async page =>
            await page.Locator("#consentNotice").IsVisibleAsync(), scripts: false);

        visible.Should().BeTrue(
            "bilgilendirmeyi görünür yapan şey betik olursa sayfa boyandıktan sonra üstüne düşer; " +
            "sunucu onu açık basmalı");
    }

    [SkippableFact]
    public async Task Onceden_kapatmis_ziyaretcide_bilgilendirme_hic_gorunmez()
    {
        var (flashed, cookieCarried) = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/Account/Login", async page =>
        {
            await page.EvaluateAsync("() => localStorage.setItem('ft.consent', '1')");
            await page.EvaluateAsync("() => document.cookie = 'ft.consent=; Max-Age=0; Path=/'");

            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Commit });
            var seen = await page.Locator("#consentNotice").IsVisibleAsync();

            await page.WaitForLoadStateAsync(LoadState.Load);
            var cookies = await page.EvaluateAsync<string>("() => document.cookie");

            return (seen, cookies.Contains("ft.consent=1"));
        });

        flashed.Should().BeFalse(
            "eski ziyaretçinin kapatma kaydı yalnızca localStorage'da duruyor; sunucu bilgilendirmeyi yine basar ve " +
            "gizleyen kural ilk boyamadan önce işlemezse bir görünüp kaybolur");
        cookieCarried.Should().BeTrue(
            "kapatma çereze taşınmazsa sunucu her istekte bilgilendirmeyi basmaya devam eder");
    }
}
