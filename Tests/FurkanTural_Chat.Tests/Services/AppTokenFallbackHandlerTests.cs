using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using FurkanTural_Chat;
using Moq;

namespace FurkanTural_Chat.Tests.Services;

public class AppTokenFallbackHandlerTests
{
    private sealed class Capture : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static (HttpClient Client, Capture Inner, Mock<IAppTokenService> Tokens) Create(string token = "uygulama-jetonu")
    {
        var tokens = new Mock<IAppTokenService>();
        tokens.Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var inner = new Capture();
        var handler = new AppTokenFallbackHandler(tokens.Object) { InnerHandler = inner };
        return (new HttpClient(handler) { BaseAddress = new Uri("http://api.test") }, inner, tokens);
    }

    [Fact]
    public async Task Kimliksiz_giris_istegine_uygulama_jetonu_eklenir()
    {
        var (client, inner, _) = Create();

        await client.PostAsync("/api/v1/Auth/login", new StringContent("{}"));

        inner.Authorization.Should().NotBeNull(
            "API ziyaretçinin IP'sine yalnızca bizim sitelerimizin jetonunu taşıyan isteklerde güvenir");
        inner.Authorization!.Scheme.Should().Be("Bearer");
        inner.Authorization.Parameter.Should().Be("uygulama-jetonu");
    }

    [Fact]
    public async Task Oturum_jetonu_tasiyan_istek_degistirilmez()
    {
        var (client, inner, tokens) = Create();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/User/me/deactivate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "kullanici-jetonu");

        await client.SendAsync(request);

        inner.Authorization!.Parameter.Should().Be("kullanici-jetonu",
            "hesap kapatma kullanıcının kendi kimliğiyle yapılır; uygulama jetonu onu ezerse istek başka birinin adına gider");
        tokens.Verify(t => t.GetTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Jeton_alinamazsa_istek_yine_gonderilir()
    {
        var (client, inner, _) = Create(token: "");

        var response = await client.PostAsync("/api/v1/Auth/login", new StringContent("{}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "jeton servisi düşse de kullanıcı giriş yapabilmeli");
        inner.Authorization.Should().BeNull();
    }
}
