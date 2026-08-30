using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class ConsentGateTests(LiveSiteFixture site)
{
    [SkippableFact]
    public async Task Ilk_ziyarette_cerez_onayi_karsilar()
    {
        var open = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/", async page =>
            await page.Locator("#consentOverlay.open").IsVisibleAsync());

        open.Should().BeTrue(
            "çerez onayı ilk ziyarette açılmazsa hiç sorulmamış olur");
    }

    [SkippableFact]
    public async Task Onay_verildiginde_kapanir_ve_bir_daha_sorulmaz()
    {
        var stillOpen = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/", async page =>
        {
            await page.ClickAsync("#consentOk");
            await page.Locator("#consentOverlay.open").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });

            await page.GotoAsync(SiteMap.Chat.BaseUrl + "/Account/Login",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });

            return await page.Locator("#consentOverlay.open").IsVisibleAsync();
        });

        stillOpen.Should().BeFalse(
            "onay bir kez verildikten sonra her sayfada yeniden sorulursa kullanıcı formlara ulaşamaz");
    }

    [SkippableFact]
    public async Task Onay_katmani_giris_formunu_engeller()
    {
        var blocked = await site.WithFirstTimeVisitorAsync(SiteMap.Chat, "/Account/Login", async page =>
            await page.EvaluateAsync<bool>(
                """
                () => {
                  const b = document.querySelector("form#loginForm button[type='submit']");
                  if (!b) return false;
                  const r = b.getBoundingClientRect();
                  const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
                  return !!(hit && hit.closest('#consentOverlay'));
                }
                """));

        blocked.Should().BeTrue(
            "onay katmanı formu gerçekten örtmelidir; örtmüyorsa kullanıcı onay vermeden giriş deneyebilir " +
            "ve bu testin diğer iki iddiası da anlamını yitirir");
    }
}
