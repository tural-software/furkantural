using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class FlowTests(LiveSiteFixture site)
{
    [SkippableFact]
    public async Task Blogda_arama_sonuc_sayfasina_goturur()
    {
        var (url, heading, echoed) = await site.WithPageAsync(SweepData.Page("Blog/"), async page =>
        {
            await page.FillAsync(".blog-search__input", "test");
            await page.ClickAsync(".blog-search__btn");
            await page.WaitForLoadStateAsync(LoadState.Load);

            var value = await page.InputValueAsync(".blog-search__input");
            var h1 = await page.Locator("h1").First.TextContentAsync();
            return (page.Url, h1 ?? "", value);
        });

        url.Should().Contain("search=test", "arama sorgusu adrese taşınmazsa sonuç paylaşılamaz ve geri tuşu bozulur");
        echoed.Should().Be("test", "arama kutusu ne aradığını göstermeye devam etmeli");
        heading.Should().NotBeEmpty("sonuç sayfasının da bir başlığı olmalı");
    }

    [SkippableFact]
    public async Task Portfolyoda_proje_karti_detaya_goturur()
    {
        var (before, after, heading) = await site.WithPageAsync(SweepData.Page("Portfolio/"), async page =>
        {
            var link = page.Locator("a[href*='/Projects/Detail/']").First;
            Skip.If(await link.CountAsync() == 0, "Portfolio: listelenmiş proje yok");

            var start = page.Url;
            await link.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);

            var h1 = await page.Locator("h1").First.TextContentAsync();
            return (start, page.Url, h1 ?? "");
        });

        after.Should().NotBe(before, "proje kartına tıklamak detay sayfasını açmalı");
        after.Should().Contain("/Projects/Detail/");
        heading.Should().NotBeEmpty("detay sayfası projeyi bir başlıkla tanıtmalı");
    }

    [SkippableFact]
    public async Task Yonetim_detay_modali_odagi_iceride_tutar_ve_escape_ile_kapanir()
    {
        var result = await site.WithPageAsync(SweepData.Page("Admin/User"), async page =>
        {
            var trigger = page.Locator("button.ft-view-btn").First;
            Skip.If(await page.Locator("button.ft-view-btn").CountAsync() == 0, "Admin/User: listelenmiş kayıt yok");

            await trigger.ClickAsync();
            await page.Locator(".dm-overlay:not(.dm-overlay--hidden)").WaitForAsync(
                new LocatorWaitForOptions { Timeout = 10000 });

            var focusInside = await page.EvaluateAsync<bool>(
                "() => !!document.activeElement && !!document.activeElement.closest('.dm-overlay')");

            await page.Keyboard.PressAsync("Escape");
            await page.Locator(".dm-overlay:not(.dm-overlay--hidden)").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 10000 });

            var closed = await page.EvaluateAsync<bool>(
                "() => { const o = document.querySelector('.dm-overlay'); return !o || o.classList.contains('dm-overlay--hidden'); }");

            return (focusInside, closed);
        });

        result.focusInside.Should().BeTrue(
            "modal açılınca odak içine taşınmalı; taşınmazsa klavye kullanıcısı hâlâ arkadaki sayfada gezer");
        result.closed.Should().BeTrue("Escape modalı kapatmalı");
    }

    [SkippableFact]
    public async Task Tema_secimi_sayfa_degisince_korunur()
    {
        var (first, second) = await site.WithPageAsync(SweepData.Page("Chat/Home/Privacy"), async page =>
        {
            var before = await page.EvaluateAsync<string>("() => document.documentElement.dataset.theme");
            await page.ClickAsync("[data-theme-toggle]");
            var after = await page.EvaluateAsync<string>("() => document.documentElement.dataset.theme");

            await page.GotoAsync(SiteMap.Chat.BaseUrl + "/Home/Agreement",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
            var carried = await page.EvaluateAsync<string>("() => document.documentElement.dataset.theme");

            after.Should().NotBe(before, "düğme temayı değiştirmeli");
            return (after, carried);
        });

        second.Should().Be(first,
            "tema seçimi sonraki sayfada da geçerli olmalı; kaybolursa kullanıcı her gezinmede " +
            "gözünü yakan bir sayfayla karşılaşır");
    }
}
