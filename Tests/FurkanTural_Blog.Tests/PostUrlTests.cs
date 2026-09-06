using FluentAssertions;
using FurkanTural_Blog.Helpers;
using FurkanTural_Blog.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Yazı adresi tek yerden üretilir. Kanonik adres <c>/yazi/{slug}</c>'dır; slug yoksa eski kimlik adresine düşülür — bağlantısız bir yazı bırakmak, eski adrese bağlanmaktan kötüdür.</summary>
public class PostUrlTests
{
    private static (IUrlHelper url, List<UrlRouteContext> routeCalls, List<UrlActionContext> actionCalls) BuildUrl()
    {
        var routeCalls = new List<UrlRouteContext>();
        var actionCalls = new List<UrlActionContext>();

        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>()))
           .Callback<UrlRouteContext>(routeCalls.Add)
           .Returns("/yazi/ornek-slug");
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
           .Callback<UrlActionContext>(actionCalls.Add)
           .Returns("/Home/Post/42");

        return (url.Object, routeCalls, actionCalls);
    }

    [Fact]
    public void Slug_varsa_kanonik_adres_kullanilir()
    {
        var (url, routeCalls, actionCalls) = BuildUrl();

        var result = PostUrl.For(url, 42, "ornek-slug");

        result.Should().Be("/yazi/ornek-slug");
        routeCalls.Should().ContainSingle();
        routeCalls[0].RouteName.Should().Be("BlogPost");
        actionCalls.Should().BeEmpty("slug varken kimlik adresi hiç üretilmemeli");
    }

    [Fact]
    public void Slug_yoksa_kimlik_adresine_dusulur()
    {
        var (url, routeCalls, actionCalls) = BuildUrl();

        var result = PostUrl.For(url, 42, "");

        result.Should().Be("/Home/Post/42");
        actionCalls.Should().ContainSingle();
        routeCalls.Should().BeEmpty();
    }

    [Fact]
    public void Yazi_kaydindan_da_ayni_adres_uretilir()
    {
        var (url, _, _) = BuildUrl();

        PostUrl.For(url, new BlogPostViewModel { Id = 42, Slug = "ornek-slug" })
            .Should().Be("/yazi/ornek-slug");
    }

    [Fact]
    public void Arsiv_kaydindan_da_ayni_adres_uretilir()
    {
        var (url, _, _) = BuildUrl();

        PostUrl.For(url, new BlogSitemapItem { Id = 42, Slug = "ornek-slug" })
            .Should().Be("/yazi/ornek-slug");
    }

    [Fact]
    public void Adres_uretilemezse_koke_dusulur()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns((string?)null);

        PostUrl.For(url.Object, 42, "ornek-slug").Should().Be("/");
    }

    [Fact]
    public void Slug_varsayilani_bostur_ve_null_olmaz()
    {
        new BlogPostViewModel().Slug.Should().BeEmpty();
        new BlogSitemapItem().Slug.Should().BeEmpty();
        new CategoryViewModel().Slug.Should().BeEmpty();
    }
}
