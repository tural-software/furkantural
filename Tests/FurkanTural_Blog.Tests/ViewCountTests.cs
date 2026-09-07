using FluentAssertions;
using FurkanTural_Blog.Controllers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Okunma sayacının sayfa tarafı. Sayaç sayfanın çizilmesiyle değil, tarayıcıdan gelen ayrı bir istekle artar: bu ayrım hem JavaScript çalıştırmayan gezginleri sayının dışında bırakır hem de aynı okuru günde bir kez saymayı mümkün kılar — tekrarı eleyen işaret tarayıcıda durur.</summary>
public class ViewCountTests
{
    private readonly Mock<IBlogApiService> _api = new();

    private HomeController Build()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = "https://api.test" })
            .Build();

        return new HomeController(_api.Object, BlogStubs.EmptyComments(), Mock.Of<IAppConfigService>(), config);
    }

    [Fact]
    public async Task Sayfa_cizilirken_sayac_artmaz()
    {
        _api.Setup(a => a.GetPostBySlugAsync("ornek", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogPostViewModel { Id = 7, Slug = "ornek", Title = "Başlık" });
        _api.Setup(a => a.GetImagesByBlogAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await Build().Detail("ornek", null, default);

        _api.Verify(a => a.RegisterViewAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never,
            "sunucuda saymak, JavaScript çalıştırmayan her gezgini okur sayardı");
    }

    [Fact]
    public async Task Sayac_ucu_istegi_apiye_iletir()
    {
        var result = await Build().RegisterView(7, default);

        result.Should().BeOfType<NoContentResult>();
        _api.Verify(a => a.RegisterViewAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sayac_yaniti_sonucu_ele_vermez()
    {
        _api.Setup(a => a.RegisterViewAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API kapalı"));

        var act = async () => await Build().RegisterView(7, default);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "istemci arızayı kendi içinde yutar; denetleyicinin ayrıca yutması, gerçek bir hatayı da sessizleştirirdi");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Bir_ve_altindaki_sayi_gosterilmez(int count)
    {
        new BlogPostViewModel { ViewCount = count }.ViewCountDisplay.Should().BeNull(
            "\"1 okuma\" yazan bir sayaç okura kendi ziyaretini gösterir; bu bilgi değildir");
    }

    [Fact]
    public void Iki_ve_ustundeki_sayi_gruplanarak_gosterilir()
    {
        new BlogPostViewModel { ViewCount = 2 }.ViewCountDisplay.Should().Be("2");
        new BlogPostViewModel { ViewCount = 1234 }.ViewCountDisplay.Should().Be("1.234");
    }
}
