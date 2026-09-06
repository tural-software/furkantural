using FluentAssertions;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Tests;

/// <summary>Etiket bağının okumaları gerçekten SQL'e çevrilebiliyor mu. Birleştirme ve gruplama içeren sorgular sahte depoyla koşan testlerde bellekte çalışır ve çevrilemeyen bir ifade ilk gerçek istekte ortaya çıkar; burada sağlayıcıya derletilirler.<para>Bağlantı açılmaz. <c>ToQueryString</c> ifadeyi derleyip SQL üretir ve çeviremediğinde istisna fırlatır.</para></summary>
public class TagQueryTranslationTests
{
    private static FurkanTuralDbContext Context()
        => new(new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .Options);

    [Fact]
    public void Yazilarin_etiketleri_tek_birlestirmeyle_cevrilebilir()
    {
        using var db = Context();
        var blogIds = new List<int> { 1, 2, 3 };

        var sql = (from bt in db.BlogTags.AsNoTracking()
                   join t in db.Tags.AsNoTracking() on bt.TagId equals t.Id
                   where blogIds.Contains(bt.BlogId)
                   select new { bt.BlogId, Tag = t })
                  .ToQueryString();

        sql.Should().Contain("[BlogTags]");
        sql.Should().Contain("[Tags]");
    }

    [Fact]
    public void Etiket_basina_yazi_sayimi_gruplanarak_cevrilebilir()
    {
        using var db = Context();
        var tagIds = new List<int> { 1, 2 };

        var sql = (from bt in db.BlogTags.AsNoTracking()
                   join b in db.Blogs.AsNoTracking() on bt.BlogId equals b.Id
                   where tagIds.Contains(bt.TagId)
                   group bt by bt.TagId into g
                   select new { TagId = g.Key, Count = g.Count() })
                  .ToQueryString();

        sql.Should().Contain("GROUP BY", "sayım veri tabanında yapılmalı; belleğe çekilen bir gruplama tüm bağ tablosunu taşırdı");
        sql.Should().Contain("[Blogs]", "sayım blog satırından geçer; pasife alınmış yazı etiketin sayısına girmemeli");
    }

    [Fact]
    public void Etiket_adresi_sorgusu_cevrilebilir()
    {
        using var db = Context();

        var sql = db.Tags.AsNoTracking().Where(t => t.Slug == "ef-core").ToQueryString();

        sql.Should().Contain("[Tags]");
        sql.Should().Contain("IsDeleted", "küresel süzgeç sorguya girmeli; silinmiş etiketin adresi açılmamalı");
    }

    [Fact]
    public void Etiket_adres_cakismasi_taramasi_cevrilebilir()
    {
        using var db = Context();
        const string basis = "ef-core";

        var sql = db.Tags
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.Slug != null && t.Slug.StartsWith(basis))
            .OrderBy(t => t.Id)
            .Select(t => new Tag { Id = t.Id, Slug = t.Slug })
            .Take(200)
            .ToQueryString();

        sql.Should().Contain("[Slug]");
        sql.Should().NotContain("IsDeleted", "çakışma taraması silinmiş satırları da görmeli; tekil indeks onları süzmez");
    }

    [Fact]
    public void Yazilarin_etiket_bagi_yazma_oncesi_okunabilir()
    {
        using var db = Context();

        var sql = db.BlogTags.Where(bt => bt.BlogId == 7).ToQueryString();

        sql.Should().Contain("[BlogTags]");
    }
}
