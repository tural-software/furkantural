using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FurkanTural_Business.Tests;

public class PushSenderStaleSubscriptionTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<PushSubscription>> _subscriptions = new();
    private List<PushSubscription>? _deleted;

    public PushSenderStaleSubscriptionTests()
    {
        _uow.SetupGet(u => u.PushSubscriptions).Returns(_subscriptions.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _subscriptions.Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<PushSubscription>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PushSubscription>, CancellationToken>((items, _) => _deleted = items.ToList())
            .Returns(Task.CompletedTask);
    }

    private PushSender Sut(string? staleAfterDays = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Push:Vapid:Subject"] = "mailto:destek@furkantural.com",
            ["Push:Vapid:PublicKey"] = "test-acik-anahtar",
            ["Push:Vapid:PrivateKey"] = "test-ozel-anahtar"
        };
        if (staleAfterDays is not null)
            settings["Push:StaleAfterDays"] = staleAfterDays;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var clock = Mock.Of<IClock>(c => c.UtcNow == Now);
        return new PushSender(_uow.Object, configuration, clock, NullLogger<PushSender>.Instance);
    }

    private void Stored(params PushSubscription[] items)
        => _subscriptions.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PushSubscription, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.AsEnumerable());

    private static PushSubscription Device(int id, DateTime createdAt, DateTime? updatedAt) => new()
    {
        Id = id,
        UserId = 7,
        Endpoint = $"https://push.invalid/{id}",
        P256dh = "gecersiz",
        Auth = "gecersiz",
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };

    [Fact]
    public async Task Otuz_gundur_sinyal_vermeyen_abonelik_gonderilmeden_silinir()
    {
        Stored(Device(1, Now.AddDays(-90), Now.AddDays(-31)));

        await Sut().SendMessageNotificationAsync(7, "Ayşe");

        _deleted.Should().NotBeNull("sayfayı kapatıp bir daha açmayan cihaza bildirim göndermek boşa iştir");
        _deleted!.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public async Task Hic_yenilenmemis_abonelik_olusturulma_tarihine_gore_eskir()
    {
        Stored(Device(2, Now.AddDays(-90), Now.AddDays(-2)), Device(3, Now.AddDays(-40), null));

        await Sut().SendMessageNotificationAsync(7, "Ayşe");

        _deleted.Should().NotBeNull();
        _deleted!.Should().ContainSingle().Which.Id.Should().Be(3,
            "yakın zamanda sinyal veren cihaz kalmalı, hiç sinyal vermeyen eski kayıt düşmeli");
    }

    [Fact]
    public async Task Sure_ayardan_okunur()
    {
        Stored(Device(4, Now.AddDays(-90), Now.AddDays(-10)));

        await Sut(staleAfterDays: "7").SendMessageNotificationAsync(7, "Ayşe");

        _deleted.Should().NotBeNull();
        _deleted!.Should().ContainSingle().Which.Id.Should().Be(4);
    }

    [Fact]
    public async Task Yakin_zamanda_sinyal_veren_abonelik_silinmez()
    {
        Stored(Device(5, Now.AddDays(-3), Now.AddHours(-1)));

        await Sut().SendMessageNotificationAsync(7, "Ayşe");

        _deleted.Should().BeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
