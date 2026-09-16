using FluentAssertions;
using FurkanTural_Portfolio.Helpers;

namespace FurkanTural_Portfolio.Tests;

/// <summary>Blog'daki eşiyle aynı kural: proje açıklamasındaki Markdown bağlantısı betik çalıştırmamalı, güvenli bağlantılar olduğu gibi kalmalı.</summary>
public class MarkdownLinkSafetyTests
{
    private static string Render(string markdown) => MarkdownRenderer.ToHtml(markdown).ToString()!;

    [Theory]
    [InlineData("[tıkla](javascript:alert(1))")]
    [InlineData("[tıkla](data:text/html,merhaba)")]
    [InlineData("[tıkla](vbscript:msgbox)")]
    [InlineData("![resim](javascript:alert(1))")]
    public void Tehlikeli_semali_baglanti_etkisizlesir(string markdown)
    {
        var html = Render(markdown).ToLowerInvariant();

        html.Should().NotContain("javascript:").And.NotContain("data:").And.NotContain("vbscript:");
    }

    [Theory]
    [InlineData("[site](https://furkantural.com)", "https://furkantural.com")]
    [InlineData("[posta](mailto:destek@furkantural.com)", "mailto:destek@furkantural.com")]
    [InlineData("[proje](/projeler/1)", "/projeler/1")]
    public void Guvenli_baglanti_oldugu_gibi_kalir(string markdown, string expectedHref)
        => Render(markdown).Should().Contain($"href=\"{expectedHref}\"");

    [Theory]
    [InlineData("https", "furkantural.com", null, "https://furkantural.com")]
    [InlineData("http", "www.furkantural.com", null, "https://www.furkantural.com")]
    [InlineData("https", "saldirgan.test", null, "https://furkantural.com")]
    [InlineData("https", "furkantural.com", 8443, "https://furkantural.com")]
    [InlineData("http", "localhost", 5042, "http://localhost:5042")]
    public void Site_adresi_yalnizca_bilinen_alan_adindan_kurulur(string scheme, string host, int? port, string expected)
        => SiteUrl.Base(scheme, host, port).Should().Be(expected);
}
