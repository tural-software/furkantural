using FluentAssertions;
using FurkanTural_Business.Services.Concrete;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;

namespace FurkanTural_Business.Tests;

/// <summary>Turnstile bir bot kapısıdır; yapılandırılmamışsa kapı açık değil kapalı olmalı. Eski davranış gizli anahtar yokken ya da CHANGE_ME yer tutucusundayken <c>true</c> dönüyordu: yanlış yapılandırılmış bir ortamda giriş ve kayıt hiçbir doğrulama olmadan geçerdi.</summary>
public class TurnstileVerifierTests
{
    private static IConfiguration Config(string? secret) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Turnstile:SecretKey"] = secret
        }).Build();

    private static IHttpClientFactory Fabrika(out Mock<HttpMessageHandler> handler)
    {
        handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", System.Text.Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(handler.Object);
        var fabrika = new Mock<IHttpClientFactory>();
        fabrika.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return fabrika.Object;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CHANGE_ME")]
    [InlineData("CHANGE_ME_TURNSTILE_SECRET")]
    [InlineData("change_me_lowercase")]
    public async Task Gizli_anahtar_yapilandirilmamissa_dogrulama_gecmez(string? secret)
    {
        var verifier = new TurnstileVerifier(Config(secret), Fabrika(out var handler));

        (await verifier.VerifyAsync("herhangi-bir-token", null)).Should().BeFalse(
            "yapılandırılmamış bir kapı açık kapı demek değildir; eski davranış her jetonu kabul ediyordu");

        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Jeton_bossa_dogrulama_gecmez()
    {
        var verifier = new TurnstileVerifier(Config("gercek-gizli-anahtar"), Fabrika(out _));

        (await verifier.VerifyAsync(null, null)).Should().BeFalse();
        (await verifier.VerifyAsync("", null)).Should().BeFalse();
    }

    [Fact]
    public async Task Yapilandirilmis_anahtarla_cloudflare_yaniti_kullanilir()
    {
        var verifier = new TurnstileVerifier(Config("gercek-gizli-anahtar"), Fabrika(out var handler));

        (await verifier.VerifyAsync("gecerli-token", "203.0.113.7")).Should().BeTrue(
            "doğru yapılandırmada karar Cloudflare'ın; kapı kapalı kalırsa kimse giremez");

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}
