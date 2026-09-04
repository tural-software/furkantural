using FluentAssertions;
using FurkanTural_Admin.Helpers;

namespace FurkanTural_Admin.Tests.Helpers;

/// <summary>Panelin süzgeç sözcüklerinin API sorgu dizesine çevrimi. Sözcükler görünümlerde sabit yazıldığı için burada birebir korunur; bir sözcük değişirse önce bu test kırılır, sayfa değil.</summary>
public class AdminListRequestTests
{
    [Theory]
    [InlineData("active", true)]
    [InlineData("passive", false)]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("tumu", null)]
    public void Aktiflik_sozcugu_bool_a_iner(string? word, bool? expected)
        => AdminListRequest.From(null, word, null, null, null, 1, 10).IsActive.Should().Be(expected);

    [Theory]
    [InlineData("deleted", true)]
    [InlineData("notDeleted", false)]
    [InlineData(null, null)]
    public void Silinmislik_sozcugu_bool_a_iner(string? word, bool? expected)
        => AdminListRequest.From(null, null, word, null, null, 1, 10).IsDeleted.Should().Be(expected);

    [Fact]
    public void Sayfa_degerleri_sinirlanir_ve_arama_kirpilir()
    {
        var request = AdminListRequest.From("  c# ", null, null, null, null, 0, 1000);

        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(10);
        request.Search.Should().Be("c#");
    }

    [Fact]
    public void Sorgu_dizesi_sayfa_ile_baslar_ve_kacislar()
    {
        var request = AdminListRequest.From("c# & .net", "active", "notDeleted", "2026-09-01", "2026-09-04", 2, 25)
            .With("blogId", 7)
            .With("isCover", true)
            .With("bos", null);

        var url = request.ToQueryString("/api/v1/skill/admin/paged", paged: true);

        url.Should().StartWith("/api/v1/skill/admin/paged?pageNumber=2&pageSize=25");
        url.Should().Contain("search=c%23%20%26%20.net");
        url.Should().Contain("isActive=true").And.Contain("isDeleted=false");
        url.Should().Contain("dateFrom=2026-09-01").And.Contain("dateTo=2026-09-04");
        url.Should().Contain("blogId=7").And.Contain("isCover=true");
        url.Should().NotContain("bos=");
    }

    [Fact]
    public void Suzgecsiz_sayaç_istegi_yalin_yola_doner()
    {
        AdminListRequest.Unfiltered.ToQueryString("/api/v1/skill/admin/counts", paged: false)
            .Should().Be("/api/v1/skill/admin/counts");
    }

    [Fact]
    public void Ek_parametre_ozgun_istegi_degistirmez()
    {
        var original = AdminListRequest.From("x", null, null, null, null, 1, 10);
        var extended = original.With("blogId", 3);

        original.Extras.Should().BeEmpty();
        extended.Extra("blogId").Should().Be("3");
        extended.Extra("yok").Should().BeNull();
    }

    [Fact]
    public void Ayni_anahtar_birden_cok_degerle_tekrarlanir()
    {
        var url = AdminListRequest.Unfiltered
            .WithAll("statuses", ["Reviewed", " ", null, "Dismissed"])
            .ToQueryString("/api/v1/report/admin/counts", paged: false);

        url.Should().Be("/api/v1/report/admin/counts?statuses=Reviewed&statuses=Dismissed",
            "ASP.NET dizi bağlaması anahtarın tekrarını bekler; boş değerler dizeye girmez");
    }
}
