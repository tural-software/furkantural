using FluentAssertions;
using FurkanTural_Blog.Controllers;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FurkanTural_Blog.Tests;

/// <summary>Etiket sayfasının adresi ve süzgeci. Kategori sayfasından tek farkı eşleştirmenin API'de yapılmasıdır; bulunamayan etiket 404 döner, çünkü var olmayan bir etiketin tüm yazıları göstermesi yanlış bağlantıyı sessizce doğru göstermek olurdu.</summary>
public class TagRoutingTests
{
    private readonly Mock<IBlogApiService> _api = new();

    public TagRoutingTests()
    {
        _api.Setup(a => a.GetImagesByBlogAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _api.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _api.Setup(a => a.GetPopularTagsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private HomeController Build()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = "https://api.test" })
            .Build();

        return new HomeController(_api.Object, config);
    }

    private void TagIs(TagViewModel? tag)
        => _api.Setup(a => a.GetTagBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(tag);

    private void PageIs(PagedPostsViewModel page)
        => _api.Setup(a => a.GetPostsPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

    [Fact]
    public async Task Bulunamayan_etiket_404_doner()
    {
        TagIs(null);
        PageIs(new PagedPostsViewModel());

        var result = await Build().Tag("yok", 1, default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Etiket_sayfasi_kendi_gorunumunu_dondurur()
    {
        TagIs(new TagViewModel { Id = 4, Name = "EF Core", Slug = "ef-core" });
        PageIs(new PagedPostsViewModel());

        var result = await Build().Tag("ef-core", 1, default);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.ViewName.Should().Be("Tag");
        var model = view.Model.Should().BeOfType<PagedPostsViewModel>().Subject;
        model.Kind.Should().Be(BlogListKind.Tag);
        model.ActiveTag!.Slug.Should().Be("ef-core");
    }

    [Fact]
    public async Task Liste_etiket_kimligiyle_suzulur()
    {
        TagIs(new TagViewModel { Id = 4, Name = "EF Core", Slug = "ef-core" });
        PageIs(new PagedPostsViewModel());

        await Build().Tag("ef-core", 1, default);

        _api.Verify(a => a.GetPostsPagedAsync(
            It.IsAny<int>(), It.IsAny<int>(), null, 4, null, It.IsAny<CancellationToken>()), Times.Once,
            "etiket sayfası kategori ya da arama süzgeci uygulamamalı");
    }

    [Fact]
    public async Task Aralik_disi_sayfa_son_gecerli_sayfaya_tasinir()
    {
        TagIs(new TagViewModel { Id = 4, Name = "EF Core", Slug = "ef-core" });
        PageIs(new PagedPostsViewModel { PageNumber = 9, TotalPages = 2 });

        var result = await Build().Tag("ef-core", 9, default);

        var redirect = result.Should().BeOfType<RedirectToRouteResult>().Subject;
        redirect.RouteName.Should().Be("BlogTag");
        redirect.RouteValues!["slug"].Should().Be("ef-core");
        redirect.RouteValues!["page"].Should().Be(2);
    }

    [Fact]
    public async Task Arsiv_etiket_bulutunu_tasir()
    {
        _api.Setup(a => a.GetArchiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ArchiveViewModel());
        _api.Setup(a => a.GetPopularTagsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TagViewModel { Id = 1, Name = "EF Core", Slug = "ef-core", PostCount = 3 }]);

        var result = await Build().Archive(default);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<ArchiveViewModel>().Subject;
        model.Tags.Should().ContainSingle().Which.Slug.Should().Be("ef-core");
    }
}
