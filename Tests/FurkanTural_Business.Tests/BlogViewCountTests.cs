using FluentAssertions;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Services.Concrete;
using FurkanTural_Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

/// <summary>Okunma sayacının servis tarafı. Sayaç satır yüklenmeden artırılır: okuyup değiştirip kaydetmek iki gidiş dönüş ister ve aynı anda okuyan iki kişi aynı eski değeri okuyup aynı yeni değeri yazardı.<para>Yayında olmayan yazı 404 döner; sayacı adresine elle istek göndererek şişirmenin yolu kapalıdır. Kararı depo verir çünkü küresel süzgeç orada uygulanır.</para></summary>
public class BlogViewCountTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBlogRepository> _blogs = new();
    private readonly Mock<ILogService> _logs = new();

    private readonly BlogService _sut;

    public BlogViewCountTests()
    {
        _uow.SetupGet(u => u.Blogs).Returns(_blogs.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc));
        _sut = new BlogService(_uow.Object, new ActivityLogger(_logs.Object, Mock.Of<IHttpContextAccessor>(), clock));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Gecersiz_kimlik_sayaca_dokunmaz(int id)
    {
        var result = await _sut.RegisterViewAsync(id);

        result.StatusCode.Should().Be(404);
        _blogs.Verify(r => r.IncrementViewCountAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Yayindaki_yazinin_sayaci_artar()
    {
        _blogs.Setup(r => r.IncrementViewCountAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.RegisterViewAsync(7);

        result.Success.Should().BeTrue();
        _blogs.Verify(r => r.IncrementViewCountAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Yayinda_olmayan_yazi_404_doner()
    {
        _blogs.Setup(r => r.IncrementViewCountAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.RegisterViewAsync(7);

        result.StatusCode.Should().Be(404,
            "pasife alınmış bir yazının sayacı, adresine elle istek gönderilerek şişirilememeli");
    }

    [Fact]
    public async Task Sayac_kayit_defterine_satir_acmaz()
    {
        _blogs.Setup(r => r.IncrementViewCountAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.RegisterViewAsync(7);

        _logs.Verify(l => l.CreateAsync(It.IsAny<FurkanTural_Application.DTOs.Log.CreateLogDto>(), It.IsAny<CancellationToken>()), Times.Never,
            "her sayfa görüntülemesi bir satır açsaydı kayıt defteri kısa sürede yalnızca bu satırlardan oluşurdu");
    }

    [Fact]
    public async Task Sayac_icin_ayrica_kaydetme_cagrilmaz()
    {
        _blogs.Setup(r => r.IncrementViewCountAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.RegisterViewAsync(7);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never,
            "artış değişiklik izleyicisinden geçmez; deponun kendi deyimi kaydeder");
    }

    [Fact]
    public void Sayi_dtolara_tasinir()
    {
        var entity = new Blog { Id = 7, Title = "Yazı", Slug = "yazi", ViewCount = 412 };

        FurkanTural_Business.Mappers.BlogMapper.ToDto(entity).ViewCount.Should().Be(412);
        FurkanTural_Business.Mappers.BlogMapper.ToAdminDto(entity).ViewCount.Should().Be(412);
    }

    [Fact]
    public void Depo_sozlesmesi_artisi_tek_deyime_birakir()
    {
        var method = typeof(IBlogRepository).GetMethod(nameof(IBlogRepository.IncrementViewCountAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<bool>),
            "satırın gerçekten güncellenip güncellenmediğini yalnızca depo bilir; çağıran kararı ondan okur");
        method.GetParameters().Select(p => p.ParameterType)
            .Should().Equal([typeof(int), typeof(CancellationToken)],
            "sayaç için varlık geçirilmesi, artışın önce okuma gerektirdiği anlamına gelirdi");
    }
}
