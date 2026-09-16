using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Push;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Business.Tests;

public class PushSubscriptionEndpointTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<PushSubscription>> _subscriptions = new();

    public PushSubscriptionEndpointTests()
    {
        _uow.SetupGet(u => u.PushSubscriptions).Returns(_subscriptions.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _subscriptions
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PushSubscription, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PushSubscription?)null);
    }

    private PushSubscriptionService Sut()
        => new(_uow.Object, new ConfigurationBuilder().Build());

    private static PushSubscriptionDto Dto(string endpoint) => new()
    {
        Endpoint = endpoint,
        P256dh = "gecersiz",
        Auth = "gecersiz"
    };

    [Theory]
    [InlineData("http://fcm.googleapis.com/fcm/send/abc")]
    [InlineData("https://localhost/push")]
    [InlineData("https://127.0.0.1/push")]
    [InlineData("https://[::1]/push")]
    [InlineData("https://169.254.169.254/latest/meta-data/")]
    [InlineData("https://10.0.0.5/push")]
    [InlineData("https://172.16.0.9/push")]
    [InlineData("https://192.168.1.10/push")]
    [InlineData("adres-degil")]
    public async Task Sunucuyu_ic_aga_yonlendiren_adres_kaydedilmez(string endpoint)
    {
        var sonuc = await Sut().SubscribeAsync(7, Dto(endpoint));

        sonuc.Success.Should().BeFalse(
            "abonelik adresine isteği sunucunun kendisi atar; adres serbest bırakılırsa üye sunucuya " +
            "kendi seçtiği iç adrese istek yaptırabilir");
        _subscriptions.Verify(r => r.AddAsync(It.IsAny<PushSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("https://fcm.googleapis.com/fcm/send/abc")]
    [InlineData("https://updates.push.services.mozilla.com/wpush/v2/abc")]
    [InlineData("https://web.push.apple.com/abc")]
    public async Task Gercek_push_servisi_adresi_kaydedilir(string endpoint)
    {
        var sonuc = await Sut().SubscribeAsync(7, Dto(endpoint));

        sonuc.Success.Should().BeTrue(
            "tarayıcıların verdiği gerçek adresler de engellenirse bildirim özelliği tümden çalışmaz");
        _subscriptions.Verify(r => r.AddAsync(It.IsAny<PushSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private PushSubscription Existing(int ownerId = 9)
    {
        var existing = new PushSubscription
        {
            Id = 1, UserId = ownerId, Endpoint = "https://fcm.googleapis.com/fcm/send/abc", P256dh = "cihaz-anahtari", Auth = "cihaz-sirri"
        };
        _subscriptions
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PushSubscription, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        return existing;
    }

    [Fact]
    public async Task Cihaz_anahtarlarini_bilmeyen_baskasinin_aboneligini_devralamaz()
    {
        var existing = Existing(ownerId: 9);

        var sonuc = await Sut().SubscribeAsync(7, new PushSubscriptionDto
        {
            Endpoint = existing.Endpoint, P256dh = "uydurma", Auth = "uydurma"
        });

        sonuc.IsFailure.Should().BeTrue(
            "yalnızca adresi bilen biri aboneliği kendine bağlayıp kurbanın bildirimlerini kesebiliyordu");
        existing.UserId.Should().Be(9);
        _subscriptions.Verify(r => r.UpdateAsync(It.IsAny<PushSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ayni_cihazda_hesap_degisince_abonelik_yeni_hesaba_gecer()
    {
        var existing = Existing(ownerId: 9);

        var sonuc = await Sut().SubscribeAsync(7, new PushSubscriptionDto
        {
            Endpoint = existing.Endpoint, P256dh = "cihaz-anahtari", Auth = "cihaz-sirri"
        });

        sonuc.Success.Should().BeTrue(
            "ortak cihazda A çıkıp B girdiğinde devir engellenirse A'nın bildirimleri B'nin önüne düşmeye devam ederdi");
        existing.UserId.Should().Be(7);
    }
}
