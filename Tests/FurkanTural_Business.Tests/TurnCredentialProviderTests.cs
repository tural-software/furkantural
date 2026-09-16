using System.Net;
using System.Text;
using FluentAssertions;
using FurkanTural_Business.Services.Concrete;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Business.Tests;

public class TurnCredentialProviderTests
{
    private sealed class Capture : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"iceServers":[{"urls":["turn:turn.cloudflare.com:3478?transport=udp"],"username":"u","credential":"c"}]}""",
                    Encoding.UTF8, "application/json")
            };
        }
    }

    private static (TurnCredentialProvider Sut, Capture Handler) Create()
    {
        var handler = new Capture();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cloudflare:Realtime:TurnKeyId"] = "anahtar",
            ["Cloudflare:Realtime:TurnApiToken"] = "jeton"
        }).Build();

        return (new TurnCredentialProvider(configuration, factory.Object), handler);
    }

    [Fact]
    public async Task Cloudflare_istegi_kullanici_kimligi_tasimaz_ve_arama_bilgileri_gelir()
    {
        var (sut, handler) = Create();

        var result = await sut.GetIceServersAsync();

        result.Success.Should().BeTrue("sesli ve görüntülü arama bu bilgilerle kurulur");
        result.Data!.IceServers.Should().ContainSingle();
        handler.Body.Should().NotBeNull();
        handler.Body.Should().NotContain("customIdentifier",
            "Cloudflare'e kullanıcı kimliği gönderilmez; alan boş bile olsa gövdede yer almamalı");
        handler.Body.Should().Contain("\"ttl\":14400",
            "sızan bir TURN kimliği bir gün boyunca aktarım sunucusunu kullandırabiliyordu; kimlik her arama başında yeniden alınıyor");
    }
}
