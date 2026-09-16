using FluentAssertions;
using FurkanTural_Blog.Helpers;

namespace FurkanTural_Blog.Tests;

/// <summary>Sitemap, besleme ve kanonik bağlantılar isteğin Host başlığından kuruluyordu. Sahte bir Host ile istenen dosya başkasının alan adını taşıyan bağlantılarla üretilip herkese açık önbelleğe girebiliyordu.</summary>
public class SiteUrlTests
{
    [Theory]
    [InlineData("https", "blog.furkantural.com", null, "https://blog.furkantural.com")]
    [InlineData("http", "BLOG.furkantural.com", null, "https://blog.furkantural.com")]
    [InlineData("https", "saldirgan.test", null, "https://blog.furkantural.com")]
    [InlineData("https", "blog.furkantural.com.saldirgan.test", null, "https://blog.furkantural.com")]
    [InlineData("https", "blog.furkantural.com", 8443, "https://blog.furkantural.com")]
    public void Canli_adres_yalnizca_bilinen_alan_adindan_kurulur(string scheme, string host, int? port, string expected)
        => SiteUrl.Base(scheme, host, port).Should().Be(expected,
            "tanınmayan her Host kanonik adrese iner; Cloudflare arkasında şema http gelebildiği için canlıda https sabittir");

    [Theory]
    [InlineData("http", "localhost", 5041, "http://localhost:5041")]
    [InlineData("https", "127.0.0.1", 7001, "https://127.0.0.1:7001")]
    public void Yerel_gelistirme_adresi_oldugu_gibi_gecer(string scheme, string host, int? port, string expected)
        => SiteUrl.Base(scheme, host, port).Should().Be(expected,
            "aksi hâlde yerelde üretilen her bağlantı canlı siteye giderdi");

    [Theory]
    [InlineData("Controllers", "SeoController.cs")]
    [InlineData("Views", "Shared", "_Layout.cshtml")]
    [InlineData("Views", "Home", "Post.cshtml")]
    [InlineData("Views", "Home", "Index.cshtml")]
    [InlineData("Views", "Home", "Category.cshtml")]
    [InlineData("Views", "Home", "Tag.cshtml")]
    public void Mutlak_adresler_Host_basligindan_dogrudan_kurulmaz(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FurkanTural.slnx")))
            directory = directory.Parent;

        var source = File.ReadAllText(Path.Combine([directory!.FullName, "Presentation", "FurkanTural_Blog", .. parts]));

        source.Should().NotContain("Request.Host}", "adres SiteUrl üzerinden kurulmalı");
    }
}
