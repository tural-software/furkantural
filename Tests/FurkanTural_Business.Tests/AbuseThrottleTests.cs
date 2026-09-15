using FluentAssertions;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Services.Concrete;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Business.Tests;

public class AbuseThrottleTests
{
    private DateTime _now = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    private AbuseThrottle Sut(Dictionary<string, string?>? settings = null)
    {
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(() => _now);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? [])
            .Build();

        return new AbuseThrottle(configuration, clock.Object);
    }

    [Fact]
    public void Esik_asilinca_reddedilir()
    {
        var sut = Sut(new Dictionary<string, string?>
        {
            ["Abuse:Contact:MaxPerWindow"] = "2",
            ["Abuse:Contact:WindowSeconds"] = "600"
        });

        sut.TryRegister(AbuseBuckets.Contact, "198.51.100.7").Should().BeTrue();
        sut.TryRegister(AbuseBuckets.Contact, "198.51.100.7").Should().BeTrue();
        sut.TryRegister(AbuseBuckets.Contact, "198.51.100.7").Should().BeFalse(
            "iletişim formu her gönderimde iki posta yolluyor; sınırsız bırakılırsa kendi SMTP'miz " +
            "keyfi adreslere ileti taşıyan bir röleye döner");
    }

    [Fact]
    public void Pencere_kayinca_yeniden_izin_verilir()
    {
        var sut = Sut(new Dictionary<string, string?>
        {
            ["Abuse:Contact:MaxPerWindow"] = "1",
            ["Abuse:Contact:WindowSeconds"] = "600"
        });

        sut.TryRegister(AbuseBuckets.Contact, "198.51.100.7").Should().BeTrue();
        sut.TryRegister(AbuseBuckets.Contact, "198.51.100.7").Should().BeFalse();

        _now = _now.AddSeconds(601);

        sut.TryRegister(AbuseBuckets.Contact, "198.51.100.7").Should().BeTrue(
            "sınır kalıcı yasak değil; pencere dolunca eski kayıtlar düşer");
    }

    [Fact]
    public void Farkli_anahtarlar_birbirini_etkilemez()
    {
        var sut = Sut(new Dictionary<string, string?> { ["Abuse:Contact:MaxPerWindow"] = "1" });

        sut.TryRegister(AbuseBuckets.Contact, "198.51.100.7").Should().BeTrue();
        sut.TryRegister(AbuseBuckets.Contact, "203.0.113.10").Should().BeTrue(
            "bir ziyaretçinin sınırı diğerini kapı dışında bırakmamalı");
    }

    [Fact]
    public void Kovalar_ayri_sayilir()
    {
        var sut = Sut(new Dictionary<string, string?>
        {
            ["Abuse:Contact:MaxPerWindow"] = "1",
            ["Abuse:Report:MaxPerWindow"] = "1"
        });

        sut.TryRegister(AbuseBuckets.Contact, "7").Should().BeTrue();
        sut.TryRegister(AbuseBuckets.Report, "7").Should().BeTrue(
            "aynı kimlik iki ayrı işlemi yapıyor olabilir; kovalar karışırsa biri diğerini tüketir");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_anahtar_sinirlanmaz(string? key)
    {
        var sut = Sut(new Dictionary<string, string?> { ["Abuse:Contact:MaxPerWindow"] = "1" });

        sut.TryRegister(AbuseBuckets.Contact, key).Should().BeTrue();
        sut.TryRegister(AbuseBuckets.Contact, key).Should().BeTrue(
            "IP çözülemediğinde isteği reddetmek, kimliği belirsiz diye herkesi engellemek olurdu");
    }
}
