using System.Security.Claims;
using FluentAssertions;
using FurkanTural_API.Hubs;
using FurkanTural_API.Realtime;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FurkanTural_API.Tests;

public class AdminLiveBroadcasterTests
{
    private readonly Mock<IClientProxy> _all = new();
    private readonly Mock<IAdminPendingWorkService> _pending = new();
    private readonly Mock<IHttpContextAccessor> _http = new();

    private AdminLiveBroadcaster Broadcaster()
    {
        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(c => c.All).Returns(_all.Object);
        var hubContext = new Mock<IHubContext<AdminHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);

        var scopes = new ServiceCollection()
            .AddSingleton(_pending.Object)
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(() => DateTime.UtcNow);

        return new AdminLiveBroadcaster(hubContext.Object, scopes, _http.Object, clock.Object, NullLogger<AdminLiveBroadcaster>.Instance);
    }

    private List<(string Event, object? Payload)> Sent() =>
        _all.Invocations
            .Where(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync))
            .Select(i => ((string)i.Arguments[0], ((object?[])i.Arguments[1]!).SingleOrDefault()))
            .ToList();

    private static ClaimsPrincipal Principal(string id, string role)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role)], "jwt"));

    [Fact]
    public void Kimlik_yalnizca_yonetici_rolunde_tasinir()
    {
        AdminLiveBroadcaster.ActorIdOf(Principal("7", "Admin")).Should().Be(7);
        AdminLiveBroadcaster.ActorIdOf(Principal("7", "User")).Should().Be(0,
            "Chatural kullanıcısının kimliği yönetici istemcisine gitmemeli; istemcinin yalnızca kendi işlemini ayırt etmesi yeter");
        AdminLiveBroadcaster.ActorIdOf(null).Should().Be(0, "arka plan işçisinin yazmasında istek yoktur");
    }

    [Fact]
    public async Task Pencere_dolunca_degisiklikler_tum_yoneticilere_gider()
    {
        var due = new[] { new AdminListChangeDto(AdminListKinds.Message, 0, 4, 1) };

        await Broadcaster().FlushAsync(due, CancellationToken.None);

        var sent = Sent().Should().ContainSingle("iş türü olmayan pencere bekleyen iş sayımı gerektirmez").Subject;
        sent.Event.Should().Be(AdminHubEvents.ListsChanged);
        sent.Payload.Should().BeEquivalentTo(new AdminListsChangedDto(due));
        _pending.Verify(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never(),
            "mesaj trafiği her pencerede üç sayım sorgusu açmamalı");
    }

    [Fact]
    public async Task Is_turu_varsa_bekleyen_sayilar_bir_kez_ve_tursuz_gider()
    {
        var snapshot = new AdminPendingWorkDto(string.Empty, 3, 1, 1);
        _pending.Setup(p => p.GetAsync(string.Empty, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);
        var due = new[]
        {
            new AdminListChangeDto(AdminListKinds.Comment, 0, 1, 0),
            new AdminListChangeDto(AdminListKinds.Report, 7, 0, 1)
        };

        await Broadcaster().FlushAsync(due, CancellationToken.None);

        Sent().Select(s => s.Event).Should().Equal(AdminHubEvents.ListsChanged, AdminHubEvents.PendingWorkChanged);
        Sent()[1].Payload.Should().BeSameAs(snapshot);
        _pending.Verify(p => p.GetAsync(string.Empty, It.IsAny<CancellationToken>()), Times.Once(),
            "rozet olayı tür taşırsa eski istemci onu yeni kayıt sanıp şerit açar; tür boş gider");
    }

    [Fact]
    public async Task Gonderim_ya_da_sayim_duserse_yayinci_hata_firlatmaz()
    {
        _all.Setup(a => a.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bağlantı yok"));
        _pending.Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("veri tabanı yok"));

        var act = () => Broadcaster().FlushAsync([new AdminListChangeDto(AdminListKinds.Contact, 0, 1, 0)], CancellationToken.None);

        await act.Should().NotThrowAsync("haber kaydın yerine geçmez; düşerse yayın döngüsü de durmamalı");
    }

    [Fact]
    public async Task Yayinlanan_degisiklik_pencere_sonunda_gonderilir()
    {
        _pending.Setup(p => p.GetAsync(string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminPendingWorkDto(string.Empty, 1, 0, 0));
        var broadcaster = Broadcaster();
        await broadcaster.StartAsync(CancellationToken.None);

        try
        {
            broadcaster.Publish([new AdminEntityChange(AdminListKinds.Comment, 1, 0)]);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline && !Sent().Any(s => s.Event == AdminHubEvents.PendingWorkChanged))
                await Task.Delay(50);
        }
        finally
        {
            await broadcaster.StopAsync(CancellationToken.None);
        }

        var lists = Sent().Where(s => s.Event == AdminHubEvents.ListsChanged).ToList();
        lists.Should().ContainSingle("tek değişiklik tek pencerede tek yayın üretir");
        lists[0].Payload.Should().BeEquivalentTo(new AdminListsChangedDto([new AdminListChangeDto(AdminListKinds.Comment, 0, 1, 0)]));
        Sent().Should().Contain(s => s.Event == AdminHubEvents.PendingWorkChanged, "yorum bir iş türüdür; rozet de tazelenmeli");
    }
}
