using FluentAssertions;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Sitemap ucu artık başlık da taşıyor; arşiv sayfası bu alandan besleniyor. Uç bilerek hafif kalır: yazı gövdesi hiç okunmaz, dolayısıyla okuma süresi buradan hesaplanamaz ve hesaplanmamalıdır — aynı yazı için yazı sayfasındakinden farklı bir sayı doğururdu.</summary>
public class BlogSitemapTests
{
    private static BlogService Build(params (int Id, string? Title, string? Slug, DateTime CreatedAt, DateTime? UpdatedAt)[] rows)
    {
        var blogs = new Mock<IBlogRepository>();
        blogs.Setup(r => r.GetSitemapDataAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rows);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Blogs).Returns(blogs.Object);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 6, 9, 0, 0, DateTimeKind.Utc));
        return new BlogService(uow.Object, new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock));
    }

    [Fact]
    public async Task Baslik_yaniya_tasinir()
    {
        var sut = Build((7, "Yazının başlığı", "yazinin-basligi", new DateTime(2026, 3, 1), null));

        var result = await sut.GetSitemapAsync();

        result.Success.Should().BeTrue();
        result.Data!.Single().Title.Should().Be("Yazının başlığı");
    }

    [Fact]
    public async Task Mevcut_alanlar_yerinde_kalir()
    {
        var created = new DateTime(2026, 3, 1);
        var updated = new DateTime(2026, 4, 2);
        var sut = Build((7, "Başlık", "baslik", created, updated));

        var dto = (await sut.GetSitemapAsync()).Data!.Single();

        dto.Id.Should().Be(7);
        dto.Slug.Should().Be("baslik");
        dto.CreatedAt.Should().Be(created);
        dto.UpdatedAt.Should().Be(updated);
    }

    [Fact]
    public async Task Basligi_bos_satir_yine_de_doner_ve_ayiklama_istemciye_kalir()
    {
        var sut = Build((7, null, "yazi", new DateTime(2026, 3, 1), null));

        (await sut.GetSitemapAsync()).Data!.Single().Title.Should().BeNull();
    }

    [Fact]
    public async Task Bos_arsiv_basarili_ve_bos_liste_doner()
    {
        var result = await Build().GetSitemapAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
