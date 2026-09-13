using FluentAssertions;
using FurkanTural_API.Hubs;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace FurkanTural_API.Tests;

/// <summary>Yönetici paneline giden canlı kanalın sunucu tarafı. Sayıları iş servisleri değil bu iki sınıf hesaplatıp gönderir; dolayısıyla rozetin doğru olup olmadığı burada belirlenir.<para>Bildirim kaydın yerine geçmez: gönderim düşerse iş akışı etkilenmemeli, çünkü yorumu, mesajı ya da şikayeti bırakan ziyaretçi yöneticinin bağlantı sorunundan habersizdir.</para></summary>
public class AdminRealtimeTests
{
    private readonly Mock<IAdminPendingWorkService> _pending = new();

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
}
