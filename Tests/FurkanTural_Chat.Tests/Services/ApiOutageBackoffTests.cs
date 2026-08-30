using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RichardSzalay.MockHttp;

namespace FurkanTural_Chat.Tests.Services;

/// <summary>API kapalıyken giriş ve kayıt sayfaları her istekte iki başarısız çağrı yapıyordu (uygulama jetonu + yapılandırma) ve bunların hiçbiri önbelleğe alınmadığı için sayfa her açılışta ~8,2 saniye bekliyordu. Başarısızlık artık kısa süre hatırlanıyor: ilk istek bedeli öder, sonrakiler anında döner.</summary>
public class ApiOutageBackoffTests
{
    private static IHttpClientFactory Fabrika(MockHttpMessageHandler handler)
    {
        var client = handler.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost:7000");

        var fabrika = new Mock<IHttpClientFactory>();
        fabrika.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return fabrika.Object;
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Api:AppKey"] = "anahtar",
            ["Api:AppName"] = "chat"
        }).Build();

    [Fact]
    public async Task Yapilandirma_alinamayinca_her_istekte_yeniden_denenmez()
    {
        var handler = new MockHttpMessageHandler();
        var istek = handler.When("*/api/v1/config/app");
        istek.Throw(new HttpRequestException("bağlantı reddedildi"));

        var servis = new AppConfigService(Fabrika(handler), NullLogger<AppConfigService>.Instance);

        for (var i = 0; i < 5; i++)
            (await servis.GetTurnstileSiteKeyAsync()).Should().BeNull();

        handler.GetMatchCount(istek).Should().Be(1,
            "beş sayfa açılışı beş başarısız çağrı yapıyordu; her biri bağlantı zaman aşımı kadar bekletiyordu");
    }

    [Fact]
    public async Task Yapilandirma_ucu_hata_dondurunce_de_geri_cekilir()
    {
        var handler = new MockHttpMessageHandler();
        var istek = handler.When("*/api/v1/config/app");
        istek.Respond(HttpStatusCode.ServiceUnavailable);

        var servis = new AppConfigService(Fabrika(handler), NullLogger<AppConfigService>.Instance);

        for (var i = 0; i < 4; i++)
            await servis.GetTurnstileSiteKeyAsync();

        handler.GetMatchCount(istek).Should().Be(1,
            "503 de bir arızadır; istisna kadar hızlı geri çekilmeli");
    }

    [Fact]
    public async Task Uygulama_jetonu_alinamayinca_her_istekte_yeniden_denenmez()
    {
        var handler = new MockHttpMessageHandler();
        var istek = handler.When("*/api/v1/Auth/app-token");
        istek.Throw(new HttpRequestException("bağlantı reddedildi"));

        var servis = new AppTokenService(Fabrika(handler), Config(), NullLogger<AppTokenService>.Instance);

        for (var i = 0; i < 5; i++)
            (await servis.GetTokenAsync()).Should().BeEmpty();

        handler.GetMatchCount(istek).Should().Be(1,
            "jeton çağrısı sayfa başına bir kez daha yapılıyordu; arıza süresince toplam bedel ikiye katlanıyordu");
    }

    [Fact]
    public async Task Basarili_yanit_geri_cekilmeyi_sifirlar()
    {
        var handler = new MockHttpMessageHandler();
        handler.When("*/api/v1/config/app").Respond("application/json", "{\"data\":{\"Turnstile:SiteKey\":\"anahtar\"}}");

        var servis = new AppConfigService(Fabrika(handler), NullLogger<AppConfigService>.Instance);

        (await servis.GetTurnstileSiteKeyAsync()).Should().Be("anahtar");
        (await servis.GetTurnstileSiteKeyAsync()).Should().Be("anahtar",
            "başarı yolunda geri çekilme devreye girmemeli; değer normal önbellekten gelir");
    }

    [Fact]
    public void Geri_cekilme_penceresi_dakikalarca_surmez()
    {
        foreach (var pencere in new[] { AppConfigService.FailureBackoff, AppTokenService.FailureBackoff })
        {
            pencere.Should().BeGreaterThan(TimeSpan.Zero, "pencere sıfırsa geri çekilme yok demektir");
            pencere.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(1),
                "API geri geldiğinde sayfa uzun süre eksik yapılandırmayla açılmamalı");
        }
    }

    [Fact]
    public async Task Ayni_anda_gelen_istekler_tek_cagri_yapar()
    {
        var handler = new MockHttpMessageHandler();
        var istek = handler.When("*/api/v1/config/app");
        istek.Respond(async _ =>
        {
            await Task.Delay(60);
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var servis = new AppConfigService(Fabrika(handler), NullLogger<AppConfigService>.Instance);

        await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => servis.GetTurnstileSiteKeyAsync()));

        handler.GetMatchCount(istek).Should().Be(1,
            "arıza anında aynı anda gelen istekler API'yi hep birlikte yoklarsa geri çekilme işe yaramaz; " +
            "ilk çağrı bitene kadar diğerleri beklemeli, sonra geri çekilmeyi görmeli");
    }
}
