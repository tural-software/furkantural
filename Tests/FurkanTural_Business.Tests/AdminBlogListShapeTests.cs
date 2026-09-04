using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Yönetici blog listesi içerik istenmediğinde içerik sütununu veri tabanından hiç okumaz: satır projeksiyonla gelir, diğer bütün alanlar aynen taşınır. İçerik istendiğinde ise eski yol değişmeden kalır; üç parametreli çağrı da o yola çıkar.</summary>
public class AdminBlogListShapeTests
{
    private readonly Mock<IBlogRepository> _blogs = new();
    private readonly BlogService _sut;
    private Expression<Func<Blog, Blog>>? _shape;
    private int _fullReads;

    public AdminBlogListShapeTests()
    {
        _blogs.Setup(r => r.SelectForAdminPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<Blog, Blog>>>(),
                It.IsAny<Expression<Func<Blog, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, Expression<Func<Blog, Blog>>, Expression<Func<Blog, bool>>?, bool, CancellationToken>(
                (_, _, s, _, _, _) => _shape = s)
            .ReturnsAsync(Array.Empty<Blog>());

        _blogs.Setup(r => r.GetAllForAdminPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<Blog, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback(() => _fullReads++)
            .ReturnsAsync(Array.Empty<Blog>());

        _blogs.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Blog, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _blogs.Setup(r => r.GetCategoriesForBlogsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<Category>>());

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Blogs).Returns(_blogs.Object);
        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 4, 9, 0, 0, DateTimeKind.Utc));
        _sut = new BlogService(uow.Object, new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock));
    }

    [Fact]
    public async Task Icerik_istenmeyince_projeksiyon_iceriksiz_ama_diger_alanlar_eksiksiz()
    {
        await _sut.GetAllForAdminPagedAsync(new AdminListQuery(), null, includeContent: false);

        _fullReads.Should().Be(0, "içerik istenmiyorsa tam satır okuyan yol hiç çağrılmamalı");
        var source = new Blog
        {
            Id = 7, Title = "Yazı", Content = new string('x', 100_000), IsActive = false, IsDeleted = true,
            CreatedAt = new DateTime(2026, 1, 2), CreatedBy = 1, UpdatedAt = new DateTime(2026, 1, 3), UpdatedBy = 2,
            DeletedAt = new DateTime(2026, 1, 4), DeletedBy = 3
        };

        var row = _shape!.Compile()(source);

        row.Content.Should().BeNull("listenin ağırlığı içerikten geliyor; projeksiyon onu okumamalı");
        row.Should().BeEquivalentTo(source, o => o.Excluding(b => b.Content),
            "içerik dışındaki her alan panelde görünür; biri düşerse tablo sessizce yanlış gösterir");
    }

    [Fact]
    public async Task Icerik_istenince_ve_eski_imzada_tam_satir_okunur()
    {
        await _sut.GetAllForAdminPagedAsync(new AdminListQuery(), null, includeContent: true);
        await _sut.GetAllForAdminPagedAsync(new AdminListQuery(), null);

        _fullReads.Should().Be(2, "içerik istenen iki çağrı da tam satır yolundan geçmeli");
        _shape.Should().BeNull("projeksiyon yolu bu çağrılarda kullanılmamalı");
    }
}
