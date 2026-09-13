using FluentAssertions;
using FurkanTural_API.Hubs;
using FurkanTural_API.Realtime;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FurkanTural_API.Tests;

/// <summary>Yönetici paneline giden canlı kanalın sunucu tarafı. Sayıları iş servisleri değil bu iki sınıf hesaplatıp gönderir; dolayısıyla rozetin doğru olup olmadığı burada belirlenir.<para>Bildirim kaydın yerine geçmez: gönderim düşerse iş akışı etkilenmemeli, çünkü yorumu, mesajı ya da şikayeti bırakan ziyaretçi yöneticinin bağlantı sorunundan habersizdir.</para></summary>
public class AdminRealtimeTests
{
    private readonly Mock<IAdminPendingWorkService> _pending = new();

    private static object? SentPayload(Mock<IClientProxy> proxy)
    {
        var call = proxy.Invocations.Single(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync));
        call.Arguments[0].Should().Be("PendingWorkChanged");
        return ((object?[])call.Arguments[1]!).Single();
    }

    [Fact]
    public async Task Hub_yenileme_istegi_yalnizca_cagirana_guncel_sayilari_gonderir()
    {
        var snapshot = new AdminPendingWorkDto(string.Empty, 2, 1, 0);
        _pending.Setup(p => p.GetAsync(string.Empty, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var caller = new Mock<ISingleClientProxy>();
        var clients = new Mock<IHubCallerClients>();
        clients.Setup(c => c.Caller).Returns(caller.Object);
        var context = new Mock<HubCallerContext>();
        context.Setup(c => c.ConnectionAborted).Returns(CancellationToken.None);

        var hub = new AdminHub(_pending.Object) { Clients = clients.Object, Context = context.Object };

        await hub.RefreshPendingWork();

        var call = caller.Invocations.Single(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync));
        call.Arguments[0].Should().Be("PendingWorkChanged");
        ((object?[])call.Arguments[1]!).Single().Should().BeSameAs(snapshot);
        clients.VerifyGet(c => c.All, Times.Never(),
            "yenileme bir yöneticinin kendi rozeti içindir; herkese yayılırsa her sayfa geçişi tüm panelleri gereksiz yere uyandırır");
    }

    [Fact]
    public async Task Baglanan_yoneticiye_once_kendi_kimligi_sonra_guncel_sayilar_gider()
    {
        _pending.Setup(p => p.GetAsync(string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminPendingWorkDto(string.Empty, 0, 0, 0));

        var caller = new Mock<ISingleClientProxy>();
        var clients = new Mock<IHubCallerClients>();
        clients.Setup(c => c.Caller).Returns(caller.Object);
        var context = new Mock<HubCallerContext>();
        context.Setup(c => c.UserIdentifier).Returns("7");

        var hub = new AdminHub(_pending.Object) { Clients = clients.Object, Context = context.Object };

        await hub.OnConnectedAsync();

        var sends = caller.Invocations.Where(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync)).ToList();
        sends.Select(i => (string)i.Arguments[0]).Should().Equal(
            [AdminHubEvents.AdminSession, AdminHubEvents.PendingWorkChanged],
            "istemci kendi işlemini ayırt edebilmek için kimliğini ilk olaydan önce bilmeli");
        ((object?[])sends[0].Arguments[1]!).Single().Should().Be(new AdminSessionDto(7));
        clients.VerifyGet(c => c.All, Times.Never(), "kimlik yalnızca bağlanan yöneticiye gider");
    }

    [Fact]
    public async Task Yenileme_isteginde_tur_bos_gider_ve_bildirim_seridi_acilmaz()
    {
        _pending.Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string kind, CancellationToken _) => new AdminPendingWorkDto(kind, 0, 0, 0));

        var caller = new Mock<ISingleClientProxy>();
        var clients = new Mock<IHubCallerClients>();
        clients.Setup(c => c.Caller).Returns(caller.Object);
        var context = new Mock<HubCallerContext>();

        var hub = new AdminHub(_pending.Object) { Clients = clients.Object, Context = context.Object };
        await hub.RefreshPendingWork();

        _pending.Verify(p => p.GetAsync(string.Empty, It.IsAny<CancellationToken>()), Times.Once,
            "istemci şeridi türe bakarak açar; yenileme yeni bir iş değildir, tür taşırsa yönetici olmayan bir 'yeni kayıt' görür");
    }

    [Fact]
    public async Task Bildirici_tum_yoneticilere_turuyle_birlikte_gonderir()
    {
        var payload = new AdminPendingWorkDto(AdminWorkKinds.Report, 1, 0, 1);
        _pending.Setup(p => p.GetAsync(AdminWorkKinds.Report, It.IsAny<CancellationToken>())).ReturnsAsync(payload);

        var all = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(c => c.All).Returns(all.Object);
        var hubContext = new Mock<IHubContext<AdminHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);

        var notifier = new AdminNotifier(hubContext.Object, _pending.Object, NullLogger<AdminNotifier>.Instance);

        await notifier.NotifyPendingWorkChangedAsync(AdminWorkKinds.Report);

        SentPayload(all).Should().BeSameAs(payload);
    }

    [Fact]
    public async Task Sayim_duserse_bildirici_hata_firlatmaz_ve_bir_sey_gondermez()
    {
        _pending.Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("veri tabanı yok"));

        var all = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(c => c.All).Returns(all.Object);
        var hubContext = new Mock<IHubContext<AdminHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);

        var notifier = new AdminNotifier(hubContext.Object, _pending.Object, NullLogger<AdminNotifier>.Instance);

        var act = () => notifier.NotifyPendingWorkChangedAsync(AdminWorkKinds.Comment);

        await act.Should().NotThrowAsync(
            "bildirim kaydın yerine geçmez; düşerse ziyaretçinin yorumu hata almış gibi görünmemeli");
        all.Invocations.Should().BeEmpty();
    }
}
