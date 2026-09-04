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

/// <summary>Açılır liste sözlüğü tam satır değil kimlik ve etiket taşır. Süzgeç, sıralama ve projeksiyon veri tabanına iner; boş etiketin yerini "Blog #id" alır ki seçenek hiç görünmez kalmasın.</summary>
public class AdminOptionsTests
{
    private readonly Mock<IBlogRepository> _blogs = new();
    private readonly BlogService _sut;
    private Expression<Func<Blog, bool>>? _predicate;
    private Expression<Func<Blog, string?>>? _orderBy;
    private Expression<Func<Blog, AdminOptionDto>>? _selector;
    private int? _take;
    private IReadOnlyList<AdminOptionDto> _rows = [];

    public AdminOptionsTests()
    {
        _blogs.Setup(r => r.GetAdminOptionsAsync(
                It.IsAny<Expression<Func<Blog, bool>>?>(),
                It.IsAny<Expression<Func<Blog, string?>>>(),
                It.IsAny<Expression<Func<Blog, AdminOptionDto>>>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Blog, bool>>?, Expression<Func<Blog, string?>>, Expression<Func<Blog, AdminOptionDto>>, int?, CancellationToken>(
                (p, o, s, t, _) => { _predicate = p; _orderBy = o; _selector = s; _take = t; })
            .ReturnsAsync(() => _rows);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Blogs).Returns(_blogs.Object);
        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 4, 9, 0, 0, DateTimeKind.Utc));
        _sut = new BlogService(uow.Object, new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock));
    }

    [Fact]
    public async Task Bos_arama_yuklem_gondermez_ve_take_aynen_gecer()
    {
        await _sut.GetAdminOptionsAsync("   ", 25);

        _predicate.Should().BeNull("boş arama süzgeç değildir; yüklem üretilirse SQL'e gereksiz bir WHERE iner");
        _take.Should().Be(25);
    }

    [Fact]
    public async Task Arama_basligi_iceren_satiri_secer_basliksizi_eler()
    {
        await _sut.GetAdminOptionsAsync(" net ", null);

        var match = _predicate!.Compile();
        match(new Blog { Title = "dotnet 10" }).Should().BeTrue();
        match(new Blog { Title = "java" }).Should().BeFalse();
        match(new Blog { Title = null }).Should().BeFalse(
            "başlıksız satır arama terimini içeremez ve null üzerinde Contains çağrılmamalı");
    }

    [Fact]
    public async Task Secici_yalniz_kimlik_ve_basligi_tasir_siralama_basliga_gore()
    {
        await _sut.GetAdminOptionsAsync(null, null);

        var pick = _selector!.Compile()(new Blog { Id = 7, Title = "Yazı", Content = new string('x', 10_000) });
        pick.Should().Be(new AdminOptionDto(7, "Yazı"),
            "sözlüğün bütün amacı içeriği taşımamak; projeksiyon iki alanın dışına çıkarsa kazanım gider");
        _orderBy!.Compile()(new Blog { Title = "Yazı" }).Should().Be("Yazı");
    }

    [Fact]
    public async Task Bos_etiket_kimlikle_doldurulur_dolu_etiket_korunur()
    {
        _rows = [new AdminOptionDto(3, ""), new AdminOptionDto(4, "Dolu")];

        var result = await _sut.GetAdminOptionsAsync(null, null);

        result.Success.Should().BeTrue();
        result.Data.Should().Equal(new AdminOptionDto(3, "Blog #3"), new AdminOptionDto(4, "Dolu"));
    }
}
