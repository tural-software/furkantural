using System.Linq.Expressions;
using FluentAssertions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Tag;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Etiket servisi. Kategoriden ayrıldığı iki yer sınanır: adın tekilliği ve yazı sayısının tek okumada toplanması.<para>Ad çakışması denetimi silinmiş satırları da görmek zorundadır — tekil indeks yumuşak silmeye göre süzülmez, dolayısıyla burada geçen bir ad kaydetme anında çakışır ve kullanıcı sebebini göremediği bir hata alır.</para></summary>
public class TagServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<Tag>> _tags = new();
    private readonly Mock<IBlogRepository> _blogs = new();

    private readonly List<Tag> _rows = [];
    private readonly Dictionary<int, int> _counts = [];

    private Tag? _added;
    private readonly TagService _sut;

    public TagServiceTests()
    {
        _tags.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _rows.Where(t => !t.IsDeleted && t.IsActive).ToList());
        _tags.Setup(r => r.GetAllForAdminAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _rows.ToList());
        _tags.Setup(r => r.GetAllForAdminAsync(It.IsAny<Expression<Func<Tag, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Tag, bool>> p, CancellationToken _) => _rows.Where(p.Compile()).ToList());
        _tags.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => _rows.FirstOrDefault(t => t.Id == id && !t.IsDeleted && t.IsActive));
        _tags.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Tag, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Tag, bool>> p, CancellationToken _) =>
                _rows.Where(t => !t.IsDeleted && t.IsActive).FirstOrDefault(p.Compile()));
        _tags.Setup(r => r.SelectForAdminPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<Tag, Tag>>>(),
                It.IsAny<Expression<Func<Tag, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, int __, Expression<Func<Tag, Tag>> shape, Expression<Func<Tag, bool>>? p, bool ___, CancellationToken ____) =>
                _rows.Where(t => p is null || p.Compile()(t)).Select(shape.Compile()).ToList());
        _tags.Setup(r => r.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
            .Callback<Tag, CancellationToken>((t, _) => { t.Id = 11; _added = t; _rows.Add(t); })
            .Returns(Task.CompletedTask);
        _tags.Setup(r => r.UpdateAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _blogs.Setup(r => r.GetPostCountsForTagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                _counts.Where(kv => ids.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

        _uow.SetupGet(u => u.Tags).Returns(_tags.Object);
        _uow.SetupGet(u => u.Blogs).Returns(_blogs.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc));
        _sut = new TagService(_uow.Object, new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock));
    }

    private Tag Row(int id, string name, string slug, bool isDeleted = false, bool isActive = true, int postCount = 0)
    {
        var tag = new Tag { Id = id, Name = name, Slug = slug, IsDeleted = isDeleted, IsActive = isActive };
        _rows.Add(tag);
        if (postCount > 0) _counts[id] = postCount;
        return tag;
    }

    [Fact]
    public async Task Yeni_etiket_addan_adres_uretir()
    {
        var result = await _sut.CreateAsync(new CreateTagDto { Name = "  EF Core  " });

        result.Success.Should().BeTrue();
        _added!.Name.Should().Be("EF Core", "ad kırpılarak saklanmalı");
        _added.Slug.Should().Be("ef-core");
    }

    [Fact]
    public async Task Ayni_ad_ikinci_kez_acilamaz()
    {
        Row(1, "EF Core", "ef-core");

        var result = await _sut.CreateAsync(new CreateTagDto { Name = "EF Core" });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        _added.Should().BeNull();
    }

    [Fact]
    public async Task Silinmis_etiketin_adi_yeniden_kullanilamaz()
    {
        Row(1, "EF Core", "ef-core", isDeleted: true);

        var result = await _sut.CreateAsync(new CreateTagDto { Name = "EF Core" });

        result.Success.Should().BeFalse("tekil indeks yumuşak silmeye göre süzülmez; kaydetme anında çakışırdı");
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Cakisan_adres_sayi_ekleyerek_tekillesir()
    {
        Row(1, "Dotnet", "net");

        var result = await _sut.CreateAsync(new CreateTagDto { Name = ".NET" });

        result.Success.Should().BeTrue();
        _added!.Slug.Should().Be("net-2");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Adsiz_etiket_kabul_edilmez(string name)
    {
        var result = await _sut.CreateAsync(new CreateTagDto { Name = name });

        result.Success.Should().BeFalse();
        _added.Should().BeNull();
    }

    [Fact]
    public async Task Ad_degisince_adres_kendiliginden_degismez()
    {
        var tag = Row(1, "EF Core", "ef-core");

        var result = await _sut.UpdateAsync(new UpdateTagDto { Id = 1, Name = "Entity Framework" });

        result.Success.Should().BeTrue();
        tag.Name.Should().Be("Entity Framework");
        tag.Slug.Should().Be("ef-core", "adresi değiştirmek dışarıdaki her bağlantıyı koparır");
    }

    [Fact]
    public async Task Adres_elle_verilirse_temizlenerek_yazilir()
    {
        var tag = Row(1, "EF Core", "ef-core");

        await _sut.UpdateAsync(new UpdateTagDto { Id = 1, Name = "EF Core", Slug = "Elle Verilen Ad!!" });

        tag.Slug.Should().Be("elle-verilen-ad");
    }

    [Fact]
    public async Task Kendi_adiyla_guncelleme_cakisma_saymaz()
    {
        Row(1, "EF Core", "ef-core");

        var result = await _sut.UpdateAsync(new UpdateTagDto { Id = 1, Name = "EF Core" });

        result.Success.Should().BeTrue("bir etiket kendisiyle çakışmaz");
    }

    [Fact]
    public async Task Bulut_yazisi_olmayan_etiketi_gostermez()
    {
        Row(1, "EF Core", "ef-core", postCount: 5);
        Row(2, "Bos", "bos");
        Row(3, "Dapper", "dapper", postCount: 2);

        var result = await _sut.GetPopularAsync(0);

        result.Data!.Select(t => t.Name).Should().Equal("EF Core", "Dapper");
    }

    [Fact]
    public async Task Bulutta_esit_sayidakiler_ada_gore_siralanir()
    {
        Row(1, "Zamanlayici", "zamanlayici", postCount: 3);
        Row(2, "Ayrisim", "ayrisim", postCount: 3);

        var result = await _sut.GetPopularAsync(0);

        result.Data!.Select(t => t.Name).Should().Equal(new[] { "Ayrisim", "Zamanlayici" },
            "sayı eşitse sıra okuma sırasına göre değişmemeli");
    }

    [Fact]
    public async Task Bulut_istenen_sayidan_fazlasini_dondurmez()
    {
        for (var i = 1; i <= 10; i++)
            Row(i, $"Etiket{i}", $"etiket{i}", postCount: i);

        var result = await _sut.GetPopularAsync(3);

        result.Data!.Should().HaveCount(3);
    }

    [Fact]
    public async Task Bulut_tavani_asilamaz()
    {
        for (var i = 1; i <= TagService.MaxPopular + 5; i++)
            Row(i, $"Etiket{i}", $"etiket{i}", postCount: 1);

        var result = await _sut.GetPopularAsync(int.MaxValue);

        result.Data!.Should().HaveCount(TagService.MaxPopular);
    }

    [Fact]
    public async Task Yonetim_listesi_yazi_sayisini_tek_okumada_toplar()
    {
        Row(1, "EF Core", "ef-core", postCount: 5);
        Row(2, "Dapper", "dapper");
        _tags.Setup(r => r.GetAllForAdminPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Tag, bool>>?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _rows.ToList());
        _tags.Setup(r => r.CountForAdminAsync(It.IsAny<Expression<Func<Tag, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _sut.GetAllForAdminPagedAsync(new AdminListQuery());

        result.Data!.Single(t => t.Id == 1).PostCount.Should().Be(5);
        result.Data!.Single(t => t.Id == 2).PostCount.Should().Be(0);
        _blogs.Verify(r => r.GetPostCountsForTagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Once,
            "etiket başına ayrı sayım, listenin kendisi kadar sorgu açardı");
    }

    [Fact]
    public async Task Adresle_okuma_bulunamayinca_404_doner()
    {
        Row(1, "EF Core", "ef-core");

        var found = await _sut.GetBySlugAsync("ef-core");
        var missing = await _sut.GetBySlugAsync("yok");

        found.Success.Should().BeTrue();
        missing.StatusCode.Should().Be(404);
    }
}
