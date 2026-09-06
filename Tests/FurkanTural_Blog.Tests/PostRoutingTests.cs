using FluentAssertions;
using FurkanTural_Blog.Controllers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Yazı adreslerinin yönlendirmesi. Kanonik adres <c>/yazi/{slug}</c>'dır ve eski kimlik adresi oraya <b>kalıcı</b> olarak taşınır — geçici yönlendirme olsaydı arama motoru eski adresi tutmaya devam eder, aynı yazı iki adresten görünürdü.</summary>
public class PostRoutingTests
{
    private static HomeController Build(Mock<IBlogApiService> api)
    {
        api.Setup(a => a.GetImagesByBlogAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync([]);
        api.Setup(a => a.GetPostsPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PagedPostsViewModel());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = "https://api.test" })
            .Build();

        return new HomeController(api.Object, config);
    }

    private static BlogPostViewModel Post(int id, string slug) =>
        new() { Id = id, Slug = slug, Title = "Başlık", Content = "İçerik" };

    [Fact]
    public async Task Kimlik_adresi_sluga_kalici_yonlendirir()
    {
        var api = new Mock<IBlogApiService>();
        api.Setup(a => a.GetPostAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(Post(42, "ornek-slug"));

        var result = await Build(api).Post(42, default);

        var redirect = result.Should().BeOfType<RedirectToRouteResult>().Subject;
        redirect.Permanent.Should().BeTrue("kalıcı taşıma sinyali verilmezse eski adres dizinde kalır");
        redirect.RouteName.Should().Be("BlogPost");
        redirect.RouteValues!["slug"].Should().Be("ornek-slug");
    }

    [Fact]
    public async Task Olmayan_kimlik_404_verir()
    {
        var api = new Mock<IBlogApiService>();
        api.Setup(a => a.GetPostAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((BlogPostViewModel?)null);

        (await Build(api).Post(42, default)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Slugu_olmayan_yazi_kimlik_adresinden_cizilir()
    {
        var api = new Mock<IBlogApiService>();
        api.Setup(a => a.GetPostAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(Post(42, ""));

        var result = await Build(api).Post(42, default);

        result.Should().BeOfType<ViewResult>("adressiz bir yönlendirme okuru hiçbir yere götürmezdi");
        ((ViewResult)result).ViewName.Should().Be("Post");
    }

    [Fact]
    public async Task Slug_adresi_yaziyi_cizer()
    {
        var api = new Mock<IBlogApiService>();
        api.Setup(a => a.GetPostBySlugAsync("ornek-slug", It.IsAny<CancellationToken>())).ReturnsAsync(Post(42, "ornek-slug"));

        var result = await Build(api).Detail("ornek-slug", default);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.ViewName.Should().Be("Post");
        view.Model.Should().BeOfType<BlogPostViewModel>().Which.Slug.Should().Be("ornek-slug");
    }

    [Fact]
    public async Task Olmayan_slug_404_verir_ve_kimlige_dusmez()
    {
        var api = new Mock<IBlogApiService>();
        api.Setup(a => a.GetPostBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((BlogPostViewModel?)null);

        var result = await Build(api).Detail("olmayan", default);

        result.Should().BeOfType<NotFoundResult>();
        api.Verify(a => a.GetPostAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Slug_adresi_yaziyi_kimlikle_bir_kez_daha_istemez()
    {
        var api = new Mock<IBlogApiService>();
        api.Setup(a => a.GetPostBySlugAsync("ornek-slug", It.IsAny<CancellationToken>())).ReturnsAsync(Post(42, "ornek-slug"));

        await Build(api).Detail("ornek-slug", default);

        api.Verify(a => a.GetPostAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
