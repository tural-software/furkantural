using FluentAssertions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Business.Helpers;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Tests;

/// <summary>Yönetici süzgeçlerinin yüklem üreticisi. Yüklemler derlenip bellek içi örneklere uygulanır; burada ölçülen şey SQL'e çeviri değil, süzgecin anlamıdır — hangi satır girer, hangisi girmez.</summary>
public class AdminFiltersTests
{
    private static readonly DateTime Day = new(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Skill[] Rows =
    [
        new() { Id = 1, Name = "C#", IsActive = true, IsDeleted = false, CreatedAt = Day.AddDays(-3) },
        new() { Id = 2, Name = "SQL", IsActive = false, IsDeleted = false, CreatedAt = Day.AddDays(-1) },
        new() { Id = 3, Name = "Docker", IsActive = false, IsDeleted = true, CreatedAt = Day.AddHours(23) },
        new() { Id = 4, Name = "Git", IsActive = true, IsDeleted = false, CreatedAt = Day.AddDays(1) }
    ];

    private static int[] Apply(AdminListQuery query)
    {
        var predicate = AdminFilters.Common<Skill>(query);
        return predicate is null
            ? Rows.Select(r => r.Id).ToArray()
            : Rows.Where(predicate.Compile()).Select(r => r.Id).ToArray();
    }

    [Fact]
    public void Suzgec_verilmezse_yuklem_uretmez()
    {
        AdminFilters.Common<Skill>(new AdminListQuery()).Should().BeNull(
            "boş süzgeçte depo bütün tabloyu sayfalamalı; gereksiz bir WHERE 1=1 üretmenin anlamı yok");
    }

    [Fact]
    public void Aktiflik_ve_silinmislik_birlikte_uygulanir()
    {
        Apply(new AdminListQuery { IsActive = false, IsDeleted = false }).Should().Equal(2);
        Apply(new AdminListQuery { IsDeleted = true }).Should().Equal(3);
        Apply(new AdminListQuery { IsActive = true }).Should().Equal(1, 4);
    }

    [Fact]
    public void Bitis_tarihi_gunun_tamamini_kapsar()
    {
        Apply(new AdminListQuery { DateFrom = Day, DateTo = Day }).Should().Equal(new[] { 3 },
            "04.09 seçildiğinde 04.09 23:00'da açılan kayıt da girmeli; sınır bir sonraki günün başlangıcıdır");
        Apply(new AdminListQuery { DateFrom = Day.AddDays(-2) }).Should().Equal(2, 3, 4);
    }

    [Fact]
    public void Yuklemler_parametre_degistirerek_birlesir()
    {
        var combined = AdminFilters.Common<Skill>(new AdminListQuery { IsActive = true })
            .AndAlso(x => x.Name != null && x.Name.StartsWith("G"));

        Rows.Where(combined.Compile()).Select(r => r.Id).Should().Equal(4);
        combined.Parameters.Should().HaveCount(1,
            "iki yüklem tek parametre altında birleşmeli; Expression.Invoke kullanılsaydı EF çeviremezdi");
    }

    [Fact]
    public void Veya_birlesimi_iki_sutunu_birden_tarar()
    {
        System.Linq.Expressions.Expression<Func<Skill, bool>>? none = null;
        var either = none.OrElse(x => x.Name == "C#").OrElse(x => x.Name == "Git");

        Rows.Where(either.Compile()).Select(r => r.Id).Should().Equal(1, 4);
    }

    [Fact]
    public void Sayfa_degerleri_sinirlanir()
    {
        new AdminListQuery { PageNumber = 0, PageSize = 500 }.SafePageNumber.Should().Be(1);
        new AdminListQuery { PageNumber = 0, PageSize = 500 }.SafePageSize.Should().Be(AdminListQuery.DefaultPageSize,
            "100'ün üstünde sayfa boyu 'hepsini getir' demektir; tam da yasaklanan şey");
        new AdminListQuery { PageSize = 100 }.SafePageSize.Should().Be(100);
        new AdminListQuery { Search = "  sql " }.SearchTerm.Should().Be("sql");
        new AdminListQuery { Search = "   " }.SearchTerm.Should().BeNull();
    }
}
