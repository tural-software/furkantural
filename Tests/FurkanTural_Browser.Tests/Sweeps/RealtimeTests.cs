using FluentAssertions;
using FurkanTural_Browser.Tests.Infrastructure;

namespace FurkanTural_Browser.Tests.Sweeps;

[Collection(BrowserSweep.Collection)]
public sealed class RealtimeTests(LiveSiteFixture site)
{
    [SkippableFact]
    public async Task Sohbet_ekrani_gercek_bir_websocket_acar()
    {
        var snapshot = await site.SnapshotAsync(SweepData.Page("Chat/Chat"), Viewport.Desktop, Themes.Dark);

        snapshot.WebSockets.Should().NotBeEmpty(
            "sohbet ekranı SignalR bağlantısını WebSocket ile kurmalı. Hiç soket açılmadıysa " +
            "SignalR sessizce long-polling'e düşmüştür; sayfa çalışır görünür ama gerçek zamanlı " +
            "taşıma kaybolmuştur ve konsolda hiçbir hata çıkmaz." + snapshot.Report(snapshot.WebSockets));

        snapshot.WebSockets.Should().Contain(url => url.Contains("/bff/hubs/chat", StringComparison.OrdinalIgnoreCase),
            "soket BFF üzerinden same-origin açılmalı; doğrudan API'ye açılırsa kullanıcının " +
            "JWT'si tarayıcıya taşınmış olur:" + snapshot.Report(snapshot.WebSockets));
    }
}
