using FluentAssertions;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Tests;

/// <summary>Silmenin denetim izi. Zaman damgasını kaydetme anında AuditSaveChangesInterceptor basar; kimliği ise servis taşır ve depo yazar, çünkü interceptor isteği yapan kullanıcıyı bilmez. Testler bağlantı açmaz — iki metot da yalnızca varlığı işaretler, veri tabanına gitmez.</summary>
public class SoftDeleteAuditTests
{
    private static Repository<Blog> Repository()
    {
        var options = new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .Options;

        return new Repository<Blog>(new FurkanTuralDbContext(options));
    }

    [Fact]
    public async Task Yumusak_silme_kimin_sildigini_yazar()
    {
        var entity = new Blog { Id = 1 };

        await Repository().SoftDeleteAsync(entity, deletedBy: 42);

        entity.IsDeleted.Should().BeTrue();
        entity.IsActive.Should().BeFalse();
        entity.DeletedBy.Should().Be(42,
            "denetim izi silme adımında kopmamalı; DeletedAt ne zaman olduğunu söylüyor ama kimin " +
            "yaptığını yalnızca bu alan söyler");
    }

    [Fact]
    public async Task Kimlik_bilinmiyorsa_alan_bos_kalir()
    {
        var entity = new Blog { Id = 1 };

        await Repository().SoftDeleteAsync(entity, deletedBy: null);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().BeNull(
            "oturum açmamış bir ziyaretçinin tetiklediği silmede (abonelikten çıkma gibi) uydurma " +
            "bir kimlik yazmak izi doğru değil yanlış yapar");
    }

    [Fact]
    public async Task Geri_alma_silen_kullaniciyi_da_temizler()
    {
        var entity = new Blog { Id = 1, IsDeleted = true, IsActive = false, DeletedAt = DateTime.UtcNow, DeletedBy = 42 };

        await Repository().RestoreAsync(entity);

        entity.IsDeleted.Should().BeFalse();
        entity.IsActive.Should().BeTrue();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull(
            "geri alınan kayıtta eski silme kimliği kalırsa, kayıt silinmemişken silinmiş gibi okunur");
    }
}
