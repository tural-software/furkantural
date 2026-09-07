using FluentAssertions;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Tests;

/// <summary>Yorum okumaları gerçekten SQL'e çevrilebiliyor mu. Sahte depoyla koşan servis testleri sorguları bellekte çalıştırır, dolayısıyla çevrilemeyen bir ifade ilk gerçek istekte ortaya çıkar; burada sağlayıcıya derletilirler.<para>Bağlantı açılmaz. <c>ToQueryString</c> ifadeyi derleyip SQL üretir ve çeviremediğinde istisna fırlatır.</para></summary>
public class CommentQueryTranslationTests
{
    private static FurkanTuralDbContext Context()
        => new(new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .Options);

    [Fact]
    public void Yazinin_onayli_kok_yorumlari_cevrilebilir()
    {
        using var db = Context();

        var sql = db.Comments
            .AsNoTracking()
            .Where(c => c.BlogId == 7 && c.ParentId == null && c.Status == CommentStatuses.Approved)
            .OrderBy(c => c.Id)
            .Take(20)
            .ToQueryString();

        sql.Should().Contain("[Comments]");
        sql.Should().Contain("IsDeleted", "küresel süzgeç sorguya girmeli; silinmiş yorum okura çizilmemeli");
    }

    [Fact]
    public void Bir_seviyenin_yanitlari_tek_sorguda_cevrilebilir()
    {
        using var db = Context();
        var parentIds = new List<int> { 1, 2, 3 };

        var sql = db.Comments
            .AsNoTracking()
            .Where(c => c.ParentId != null && parentIds.Contains(c.ParentId.Value) && c.Status == CommentStatuses.Approved)
            .ToQueryString();

        sql.Should().Contain("[ParentId]");
        sql.Should().Contain("IN", "bir seviye, yorum başına ayrı sorguyla değil tek IN ile alınmalı");
    }

    [Fact]
    public void Ayni_adresin_yakin_zamanli_yorumu_cevrilebilir()
    {
        using var db = Context();
        var cutoff = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);

        var sql = db.Comments
            .AsNoTracking()
            .Where(c => c.BlogId == 7 && c.AuthorEmail == "okur@site.test" && c.CreatedAt > cutoff)
            .ToQueryString();

        sql.Should().Contain("[AuthorEmail]");
        sql.Should().Contain("[CreatedAt]");
    }

    [Fact]
    public void Denetim_sayaci_silinmisleri_disarida_birakarak_cevrilebilir()
    {
        using var db = Context();

        var sql = db.Comments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.Status == CommentStatuses.Pending)
            .ToQueryString();

        sql.Should().Contain("[Status]");
        sql.Should().Contain("[IsDeleted]");
    }

    [Fact]
    public void Yanit_sayaci_izdusumu_cevrilebilir()
    {
        using var db = Context();
        var ids = new List<int> { 4, 5 };

        var sql = db.Comments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.ParentId != null && ids.Contains(c.ParentId.Value))
            .Select(c => new Comment { Id = c.Id, ParentId = c.ParentId })
            .ToQueryString();

        sql.Should().Contain("[ParentId]");
        sql.Should().NotContain("[Body]", "sayaç için gövdeyi çekmek, bir sayfalık listenin tamamını belleğe taşırdı");
    }

    [Fact]
    public void Bekleyen_bildirimlerin_imlecli_okumasi_cevrilebilir()
    {
        using var db = Context();

        var sql = db.CommentNotifications
            .AsNoTracking()
            .Where(n => n.Status == CommentNotificationStatuses.Pending && n.AttemptCount < 3 && n.Id > 40)
            .OrderBy(n => n.Id)
            .Take(25)
            .ToQueryString();

        sql.Should().Contain("[CommentNotifications]");
        sql.Should().Contain("[AttemptCount]");
    }

    [Fact]
    public void Cikis_jetonu_aramasi_silinmis_satirlari_da_gorur()
    {
        using var db = Context();

        var sql = db.CommentNotifications
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(n => n.TokenHash == "ozet")
            .ToQueryString();

        sql.Should().Contain("[TokenHash]");
        sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..].Should().NotContain("IsDeleted",
            "kapatma isteği, satırı sonradan silinmiş bir bildirimde de çalışmalı; okurun tercihi kayıt temizliğine bağlı olamaz");
    }
}
