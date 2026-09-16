using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using FurkanTural_Chat;
using Moq;

namespace FurkanTural_Chat.Tests.Services;

/// <summary>API, anahtarı döndürülmüş ya da listeden çıkarılmış bir uygulamanın jetonunu artık her istekte reddediyor. Ön-yüz reddedilen jetonu önbellekte tutmaya devam ederse bitişine kadar her çağrısı yetkisiz döner; bu yüzden jeton katmanının reddi (WWW-Authenticate taşıyan 401) önbelleği boşaltır.</summary>
public class AppTokenInvalidationTests
{
    private sealed class Responder(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private static HttpResponseMessage Unauthorized(bool challenge)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        if (challenge)
            response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer", "error=\"invalid_token\""));
        return response;
    }

    private static Mock<IAppTokenService> Tokens()
    {
        var tokens = new Mock<IAppTokenService>();
        tokens.Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("uygulama-jetonu");
        return tokens;
    }

    [Fact]
    public async Task Jeton_katmaninin_reddettigi_uygulama_jetonu_onbellekten_atilir()
    {
        var tokens = Tokens();
        var client = new HttpClient(new DefaultTokenHandler(tokens.Object) { InnerHandler = new Responder(Unauthorized(challenge: true)) })
            { BaseAddress = new Uri("http://api.test") };

        await client.GetAsync("/api/v1/config/app");

        tokens.Verify(t => t.Invalidate("uygulama-jetonu"), Times.Once);
    }

    [Fact]
    public async Task Yanlis_parola_401_i_uygulama_jetonunu_atmaz()
    {
        var tokens = Tokens();
        var client = new HttpClient(new AppTokenFallbackHandler(tokens.Object) { InnerHandler = new Responder(Unauthorized(challenge: false)) })
            { BaseAddress = new Uri("http://api.test") };

        await client.PostAsync("/api/v1/Auth/login", new StringContent("{}"));

        tokens.Verify(t => t.Invalidate(It.IsAny<string>()), Times.Never,
            "hatalı parolanın 401'i jetonla ilgili değildir; her başarısız girişte yeni jeton almak gereksiz yük olurdu");
    }

    [Fact]
    public async Task Kullanicinin_kendi_jetonu_reddedilince_uygulama_jetonu_atilmaz()
    {
        var tokens = Tokens();
        var client = new HttpClient(new AppTokenFallbackHandler(tokens.Object) { InnerHandler = new Responder(Unauthorized(challenge: true)) })
            { BaseAddress = new Uri("http://api.test") };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/User/me/deactivate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "kullanici-jetonu");

        await client.SendAsync(request);

        tokens.Verify(t => t.Invalidate(It.IsAny<string>()), Times.Never);
    }
}
