using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FurkanTural_Blog.Models.Wrappers;
using FurkanTural_Blog.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace FurkanTural_Blog.Tests;

/// <summary>Abonelik çağrısı. Kritik nokta metnin nereden okunduğudur: API zarfı Ok yolunda Message'ı, Fail yolunda Errors'ı doldurur ve ikisi birlikte dolmaz — yanlış alanı okumak "zaten abone listesinde" gibi bir bilgiyi ekranda boş bırakırdı.</summary>
public class NewsletterClientTests
{
    private static (NewsletterClient client, List<HttpRequestMessage> requests) Build(
        HttpStatusCode statusCode, object? body, Exception? throws = null)
    {
        var requests = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var setup = handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, _) => requests.Add(r));

        if (throws is not null)
            setup.ThrowsAsync(throws);
        else
            setup.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(body ?? new { }), Encoding.UTF8, "application/json")
            });

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.test/") };
        return (new NewsletterClient(http, NullLogger<NewsletterClient>.Instance), requests);
    }

    [Fact]
    public async Task Basarili_yanitta_message_okunur()
    {
        var (client, _) = Build(HttpStatusCode.OK,
            new ApiResult { Success = true, Message = "Abonelik başarıyla tamamlandı." });

        var outcome = await client.SubscribeAsync("okur@example.invalid", "bot-jetonu");

        outcome.Succeeded.Should().BeTrue();
        outcome.Message.Should().Be("Abonelik başarıyla tamamlandı.");
    }

    [Fact]
    public async Task Basarisiz_yanitta_metin_errors_dizisinden_okunur()
    {
        var (client, _) = Build(HttpStatusCode.BadRequest,
            new ApiResult { Success = false, StatusCode = 400, Errors = ["Bot doğrulaması başarısız. Lütfen tekrar deneyin."] });

        var outcome = await client.SubscribeAsync("okur@example.invalid", "bot-jetonu");

        outcome.Succeeded.Should().BeFalse();
        outcome.Message.Should().Be("Bot doğrulaması başarısız. Lütfen tekrar deneyin.");
    }

    [Fact]
    public async Task Errors_bossa_metin_bos_doner_ve_cagiran_kendi_yedegini_kullanir()
    {
        var (client, _) = Build(HttpStatusCode.BadRequest, new ApiResult { Success = false, StatusCode = 400 });

        var outcome = await client.SubscribeAsync("okur@example.invalid", "bot-jetonu");

        outcome.Succeeded.Should().BeFalse();
        outcome.Message.Should().BeNull();
    }

    [Fact]
    public async Task Api_ye_ulasilamazsa_istisna_disariya_sizmaz()
    {
        var (client, _) = Build(HttpStatusCode.OK, null, new HttpRequestException("Unreachable"));

        var outcome = await client.SubscribeAsync("okur@example.invalid", "bot-jetonu");

        outcome.Succeeded.Should().BeFalse();
        outcome.Message.Should().BeNull();
    }

    [Fact]
    public async Task Abonelik_ucuna_post_ile_gidilir()
    {
        var (client, requests) = Build(HttpStatusCode.OK, new ApiResult { Success = true });

        await client.SubscribeAsync("okur@example.invalid", "bot-jetonu");

        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].RequestUri!.AbsolutePath.Should().EndWith("/subscriber/subscribe");
    }

    [Theory]
    [InlineData("confirm")]
    [InlineData("request-unsubscribe")]
    [InlineData("unsubscribe")]
    public async Task Her_uc_kendi_yoluna_gider(string path)
    {
        var (client, requests) = Build(HttpStatusCode.OK, new ApiResult { Success = true });

        _ = path switch
        {
            "confirm" => await client.ConfirmAsync("jeton"),
            "request-unsubscribe" => await client.RequestUnsubscribeAsync("okur@example.invalid", "bot-jetonu"),
            _ => await client.UnsubscribeAsync("jeton")
        };

        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].RequestUri!.AbsolutePath.Should().EndWith($"/subscriber/{path}");
    }

    [Fact]
    public async Task Cikis_istegi_jeton_degil_adres_tasir()
    {
        var (client, requests) = Build(HttpStatusCode.OK, new ApiResult { Success = true });

        await client.RequestUnsubscribeAsync("okur@example.invalid", "bot-jetonu");

        var body = await requests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("okur@example.invalid");
        body.Should().Contain("bot-jetonu", "bot doğrulaması API tarafında yapılır");
        body.Should().NotContain("\"token\"", "çıkış isteği tek kullanımlık jetonu taşımaz");
    }

    [Fact]
    public async Task Cikisi_bitiren_istek_adres_degil_jeton_tasir()
    {
        var (client, requests) = Build(HttpStatusCode.OK, new ApiResult { Success = true });

        await client.UnsubscribeAsync("gizli-jeton");

        var body = await requests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("gizli-jeton");
        body.Should().NotContain("email");
    }
}
