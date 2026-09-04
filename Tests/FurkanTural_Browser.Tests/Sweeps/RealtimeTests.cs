using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class RealtimeTests(LiveSiteFixture site)
{
    [SkippableFact]
    public async Task Sohbet_ekrani_gercek_bir_websocket_acar()
    {
        var sockets = await site.WithPageAsync(SweepData.Page("Chat/Chat"), async page =>
        {
            var seen = new List<string>();
            page.WebSocket += (_, socket) => seen.Add(socket.Url);

            try
            {
                await page.RunAndWaitForWebSocketAsync(
                    () => page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 }),
                    new PageRunAndWaitForWebSocketOptions { Timeout = 20000 });
            }
            catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
            {
            }

            return seen.Distinct().ToArray();
        });

        sockets.Should().NotBeEmpty(
            "sohbet ekranı SignalR bağlantısını WebSocket ile kurmalı. Yirmi saniye içinde hiç soket açılmadıysa " +
            "SignalR sessizce long-polling'e düşmüştür; sayfa çalışır görünür ama gerçek zamanlı " +
            "taşıma kaybolmuştur ve konsolda hiçbir hata çıkmaz.");

        sockets.Should().Contain(url => url.Contains("/bff/hubs/chat", StringComparison.OrdinalIgnoreCase),
            "soket BFF üzerinden same-origin açılmalı; doğrudan API'ye açılırsa kullanıcının " +
            "JWT'si tarayıcıya taşınmış olur:" + Environment.NewLine + string.Join(Environment.NewLine, sockets.Select(s => "  - " + s)));
    }
}
