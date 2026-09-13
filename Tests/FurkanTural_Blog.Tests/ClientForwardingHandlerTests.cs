using System.Net;
using FluentAssertions;
using FurkanTural_Blog;
using Microsoft.AspNetCore.Http;

namespace FurkanTural_Blog.Tests;

public class ClientForwardingHandlerTests
{
    private sealed class Capture : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static async Task<HttpRequestMessage> Send(HttpContext? context, Action<HttpRequestMessage>? prepare = null)
    {
        var accessor = new HttpContextAccessor { HttpContext = context };
        var inner = new Capture();
        var client = new HttpClient(new ClientForwardingHandler(accessor) { InnerHandler = inner });
        var request = new HttpRequestMessage(HttpMethod.Get, "http://api.test/api/v1/blog/paged");
        prepare?.Invoke(request);

        await client.SendAsync(request);
        return inner.Request!;
    }

    private static DefaultHttpContext Visitor(string ip, string? userAgent)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        if (userAgent is not null)
            context.Request.Headers.UserAgent = userAgent;
        return context;
    }

    private static string? Header(HttpRequestMessage request, string name)
        => request.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;

    [Fact]
    public async Task Ziyaretcinin_ipsi_ve_tarayici_bilgisi_istege_eklenir()
    {
        var sent = await Send(Visitor("198.51.100.7", "Mozilla/5.0 (Ziyaretci)"));

        Header(sent, ClientForwarding.IpHeader).Should().Be("198.51.100.7");
        Header(sent, ClientForwarding.UserAgentHeader).Should().Be("Mozilla/5.0 (Ziyaretci)");
    }

    [Fact]
    public async Task IPv4_ile_eslenmis_IPv6_duz_IPv4_olarak_gider()
    {
        var sent = await Send(Visitor("::ffff:198.51.100.7", null));

        Header(sent, ClientForwarding.IpHeader).Should().Be("198.51.100.7",
            "kayıtlarda aynı ziyaretçi iki farklı yazımla görünmemeli");
        Header(sent, ClientForwarding.UserAgentHeader).Should().BeNull();
    }

    [Fact]
    public async Task ASCII_disi_tarayici_bilgisi_gonderilmez()
    {
        var sent = await Send(Visitor("198.51.100.7", "Tarayıcı Ürünü"));

        Header(sent, ClientForwarding.UserAgentHeader).Should().BeNull(
            "ASCII dışı başlık değeri isteği gönderilmeden düşürür; ziyaretçinin formu hiç ulaşmazdı");
        Header(sent, ClientForwarding.IpHeader).Should().Be("198.51.100.7");
    }

    [Fact]
    public async Task Istek_baglami_yoksa_onceden_konmus_baslik_da_temizlenir()
    {
        var sent = await Send(null, request =>
            request.Headers.TryAddWithoutValidation(ClientForwarding.IpHeader, "203.0.113.99"));

        Header(sent, ClientForwarding.IpHeader).Should().BeNull(
            "arka plan çağrılarında ziyaretçi yoktur; başka bir isteğin adresi taşınmamalı");
    }
}
