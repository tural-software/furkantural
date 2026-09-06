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

/// <summary>Toplu işlem tek kaydetmeyle biter ve yalnızca uygun durumdaki satırlara dokunur: silinmişi silmez, silinmemişi geri yüklemez, silinmişin aktifliğini değiştirmez. Atlanan ve bulunamayan kimlikler yanıtta listelenir; hiçbir satır değişmezse veri tabanına yazma da olmaz.</summary>
public class BlogServiceBulkTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBlogRepository> _blogs = new();
    private readonly List<Blog> _rows = [];
    private readonly List<(int Id, int? DeletedBy)> _softDeleted = [];
    private readonly List<int> _restored = [];
    private readonly List<int> _updated = [];
    private readonly BlogService _sut;

    public BlogServiceBulkTests()
    {
        _blogs.Setup(r => r.GetAllForAdminAsync(It.IsAny<Expression<Func<Blog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Blog, bool>> p, CancellationToken _) => _rows.Where(p.Compile()).ToList());
        _blogs.Setup(r => r.SoftDeleteAsync(It.IsAny<Blog>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback<Blog, int?, CancellationToken>((b, by, _) => { b.IsDeleted = true; b.IsActive = false; b.DeletedBy = by; _softDeleted.Add((b.Id, by)); })
            .Returns(Task.CompletedTask);
        _blogs.Setup(r => r.RestoreAsync(It.IsAny<Blog>(), It.IsAny<CancellationToken>()))
            .Callback<Blog, CancellationToken>((b, _) => { b.IsDeleted = false; b.IsActive = true; _restored.Add(b.Id); })
            .Returns(Task.CompletedTask);
        _blogs.Setup(r => r.UpdateAsync(It.IsAny<Blog>(), It.IsAny<CancellationToken>()))
            .Callback<Blog, CancellationToken>((b, _) => _updated.Add(b.Id))
            .Returns(Task.CompletedTask);

        _uow.SetupGet(u => u.Blogs).Returns(_blogs.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = Mock.Of<IClock>(c => c.UtcNow == new DateTime(2026, 9, 4, 9, 0, 0, DateTimeKind.Utc));
        _sut = new BlogService(_uow.Object, new ActivityLogger(Mock.Of<ILogService>(), Mock.Of<IHttpContextAccessor>(), clock));
    }

    private void Rows(params Blog[] rows) => _rows.AddRange(rows);

    [Fact]
    public async Task Silme_silinmemisleri_siler_silinmisi_ve_olmayani_atlar_tek_kaydeder()
    {
        Rows(new Blog { Id = 1, IsActive = true }, new Blog { Id = 2, IsActive = false }, new Blog { Id = 3, IsDeleted = true, IsActive = false });

        var result = await _sut.BulkAsync(BulkAction.Delete, [1, 2, 3, 99, 1], userId: 7);

        result.Success.Should().BeTrue();
        result.Data!.Requested.Should().Be(4, "tekrar eden kimlik bir kez sayılır");
        result.Data.Affected.Should().Be(2);
        result.Data.Skipped.Should().BeEquivalentTo([3, 99], "silinmiş satır ve olmayan kimlik atlanır ama hata değildir");
        _softDeleted.Should().BeEquivalentTo([(1, (int?)7), (2, (int?)7)], "silen kimlik her satıra yazılır");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once, "toplu işlem tek transaction'dır");
    }

    [Fact]
    public async Task Geri_yukleme_yalniz_silinmisleri_alir()
    {
        Rows(new Blog { Id = 1, IsActive = true }, new Blog { Id = 2, IsDeleted = true, IsActive = false });

        var result = await _sut.BulkAsync(BulkAction.Restore, [1, 2], userId: 7);

        _restored.Should().Equal(2);
        result.Data!.Skipped.Should().Equal(1);
        _rows.Single(b => b.Id == 2).UpdatedBy.Should().Be(7);
    }

    [Fact]
    public async Task Pasife_alma_yalniz_aktif_ve_silinmemis_satirlara_dokunur()
    {
        Rows(new Blog { Id = 1, IsActive = true }, new Blog { Id = 2, IsActive = false }, new Blog { Id = 3, IsDeleted = true, IsActive = true });

        var result = await _sut.BulkAsync(BulkAction.Deactivate, [1, 2, 3], userId: 7);

        _updated.Should().Equal(1);
        _rows.Single(b => b.Id == 1).IsActive.Should().BeFalse();
        _rows.Single(b => b.Id == 1).UpdatedBy.Should().Be(7);
        _rows.Single(b => b.Id == 3).IsActive.Should().BeTrue("silinmiş satırın aktifliğine dokunulmaz; tekil uç da aynı kuralı uygular");
        result.Data!.Skipped.Should().BeEquivalentTo([2, 3]);
    }

    [Fact]
    public async Task Hicbir_satir_degismezse_kaydetme_de_olmaz()
    {
        Rows(new Blog { Id = 1, IsActive = false });

        var result = await _sut.BulkAsync(BulkAction.Deactivate, [1], userId: 7);

        result.Success.Should().BeTrue();
        result.Data!.Affected.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never,
            "değişen satır yokken SaveChanges çağırmak boş bir transaction açar");
    }

    [Fact]
    public async Task Bos_ya_da_asiri_liste_400_doner()
    {
        var empty = await _sut.BulkAsync(BulkAction.Delete, [], userId: 7);
        var tooMany = await _sut.BulkAsync(BulkAction.Delete, Enumerable.Range(1, BulkActions.MaxBulk + 1).ToList(), userId: 7);

        empty.Success.Should().BeFalse();
        empty.StatusCode.Should().Be(400);
        tooMany.Success.Should().BeFalse();
        tooMany.StatusCode.Should().Be(400, "tavan sayfa boyu tavanıyla aynıdır; bir sayfada seçilebilecekten fazlası tek istekte işlenmez");
        _blogs.Verify(r => r.GetAllForAdminAsync(It.IsAny<Expression<Func<Blog, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
