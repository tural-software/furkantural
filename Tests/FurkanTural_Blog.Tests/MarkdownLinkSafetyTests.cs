using FluentAssertions;
using FurkanTural_Blog.Helpers;

namespace FurkanTural_Blog.Tests;

/// <summary>Ham HTML kapalı olsa da Markdown'ın kendi bağlantı söz dizimi etiket kaçışından geçer. Bir yazıya <c>[tıkla](javascript:...)</c> girildiğinde okuyucunun tıkladığı bağlantı betik çalıştırmamalı.</summary>
public class MarkdownLinkSafetyTests
{
    private static string Render(string markdown) => MarkdownRenderer.ToHtml(markdown).ToString()!;

    [Theory]
    [InlineData("[tıkla](javascript:alert(1))")]
    [InlineData("[tıkla](JavaScript:alert(1))")]
    [InlineData("[tıkla](data:text/html,merhaba)")]
    [InlineData("[tıkla](vbscript:msgbox)")]
    [InlineData("[tıkla](javascript&#58;alert(1))")]
    [InlineData("![resim](javascript:alert(1))")]
    public void Tehlikeli_semali_baglanti_etkisizlesir(string markdown)
    {
        var html = Render(markdown);

        html.ToLowerInvariant().Should().NotContain("javascript:").And.NotContain("data:").And.NotContain("vbscript:");
        html.Should().Contain("\"#\"", "bağlantı metni kalır, yalnızca adres etkisizleşir");
    }

    [Theory]
    [InlineData("[site](https://furkantural.com)", "https://furkantural.com")]
    [InlineData("[site](http://ornek.test/yol?a=1)", "http://ornek.test/yol?a=1")]
    [InlineData("[posta](mailto:destek@furkantural.com)", "mailto:destek@furkantural.com")]
    [InlineData("[arşiv](/arsiv)", "/arsiv")]
    [InlineData("[bölüm](#giris)", "#giris")]
    [InlineData("[göreli](yazi/baska-yazi)", "yazi/baska-yazi")]
    [InlineData("[sorgu](/ara?q=a:b)", "/ara?q=a:b")]
    public void Guvenli_baglanti_oldugu_gibi_kalir(string markdown, string expectedHref)
        => Render(markdown).Should().Contain($"href=\"{expectedHref}\"");

    [Fact]
    public void Otomatik_baglanti_da_denetlenir()
        => Render("<javascript:alert(1)>").ToLowerInvariant().Should().NotContain("href=\"javascript:");

    [Fact]
    public void Referans_bicimli_baglanti_da_denetlenir()
        => Render("[tıkla][x]\n\n[x]: javascript:alert(1)").ToLowerInvariant().Should().NotContain("javascript:");
}
