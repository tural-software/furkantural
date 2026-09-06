using FluentAssertions;
using FurkanTural_Blog.Helpers;
using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Tests;

/// <summary>İlgili yazı seçimi. Başlık "ilgili" dediği için listenin dolu olması yeterli değil: ortak kategorisi olmayan bir yazı listeye girerse başlık yanlış olur, dolayısıyla eksik kalan yer en yeni yazılarla doldurulmaz.</summary>
public class RelatedPostsTests
{
    private static CategoryViewModel Cat(int id) => new() { Id = id, Name = $"Kategori {id}" };

    private static BlogPostViewModel Post(int id, DateTime created, params int[] categoryIds) => new()
    {
        Id = id,
        Title = $"Yazı {id}",
        CreatedAt = created,
        Categories = [.. categoryIds.Select(Cat)]
    };

    private static readonly DateTime Day = new(2026, 5, 10);

    [Fact]
    public void Yazinin_kendisi_listeye_girmez()
    {
        var current = Post(1, Day, 7);

        var picked = RelatedPosts.Pick(current, [current, Post(2, Day, 7)]);

        picked.Select(p => p.Id).Should().Equal(2);
    }

    [Fact]
    public void Ortak_kategorisi_olmayan_aday_elenir()
    {
        var current = Post(1, Day, 7);

        var picked = RelatedPosts.Pick(current, [Post(2, Day, 9), Post(3, Day, 7)]);

        picked.Select(p => p.Id).Should().Equal(3);
    }

    [Fact]
    public void Eksik_kalan_yer_ilgisiz_yaziyla_doldurulmaz()
    {
        var current = Post(1, Day, 7);

        var picked = RelatedPosts.Pick(current, [Post(2, Day, 7), Post(3, Day, 9), Post(4, Day, 9)]);

        picked.Should().HaveCount(1);
    }

    [Fact]
    public void Cok_ortak_kategorili_aday_one_gecer()
    {
        var current = Post(1, Day, 7, 8);
        var tekOrtak = Post(2, Day.AddDays(5), 7);
        var ciftOrtak = Post(3, Day.AddDays(-30), 7, 8);

        var picked = RelatedPosts.Pick(current, [tekOrtak, ciftOrtak]);

        picked.Select(p => p.Id).Should().Equal(3, 2);
    }

    [Fact]
    public void Ortak_sayisi_esitse_yeni_olan_one_gecer()
    {
        var current = Post(1, Day, 7);

        var picked = RelatedPosts.Pick(current, [Post(2, Day.AddDays(-10), 7), Post(3, Day.AddDays(-1), 7)]);

        picked.Select(p => p.Id).Should().Equal(3, 2);
    }

    [Fact]
    public void Ayni_gunun_yazilari_kimlige_gore_kararli_siralanir()
    {
        var current = Post(1, Day, 7);

        var picked = RelatedPosts.Pick(current, [Post(5, Day, 7), Post(9, Day, 7)]);

        picked.Select(p => p.Id).Should().Equal(9, 5);
    }

    [Fact]
    public void Ayni_yazi_iki_kez_gelirse_bir_kez_gosterilir()
    {
        var current = Post(1, Day, 7);
        var aday = Post(2, Day, 7);

        var picked = RelatedPosts.Pick(current, [aday, aday]);

        picked.Should().HaveCount(1);
    }

    [Fact]
    public void Ucten_fazla_aday_kirpilir()
    {
        var current = Post(1, Day, 7);
        var adaylar = Enumerable.Range(2, 8).Select(i => Post(i, Day.AddDays(-i), 7)).ToList();

        RelatedPosts.Pick(current, adaylar).Should().HaveCount(RelatedPosts.Count);
    }

    [Fact]
    public void Kategorisiz_yazinin_ilgilisi_olmaz()
        => RelatedPosts.Pick(Post(1, Day), [Post(2, Day, 7)]).Should().BeEmpty();

    [Fact]
    public void Aday_sayfasi_gosterilecek_karttan_bir_fazla_istenir()
        => RelatedPosts.CandidatePageSize.Should().Be(RelatedPosts.Count + 1);
}
