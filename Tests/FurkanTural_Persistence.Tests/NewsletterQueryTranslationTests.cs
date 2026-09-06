using FluentAssertions;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Tests;

/// <summary>Bülten dağıtımının sorguları gerçekten SQL'e çevrilebiliyor mu. Sahte depoyla koşan birim testleri bunu göremez: orada yüklem derlenip belleğe uygulanır, sağlayıcıya hiç gitmez, dolayısıyla çevrilemeyen bir ifade ilk gerçek gönderime kadar saklanır.<para>Sayılar burada sabittir ve dağıtıcının ayarlarına bağlanmaz: sınanan şey eşiğin değeri değil, o biçimdeki bir yüklemin çevrilebilmesidir.</para><para>Bağlantı açılmaz. <c>ToQueryString</c> ifadeyi derleyip SQL üretir ve çeviremediğinde istisna fırlatır; sınanan şey sonuç değil, sorgunun kurulabilmesidir.</para></summary>
public class NewsletterQueryTranslationTests
{
    private static FurkanTuralDbContext Context()
        => new(new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .Options);

    [Fact]
    public void Dagitilacak_sayilar_sorgusu_cevrilebilir()
    {
        using var db = Context();

        var sql = db.NewsletterIssues
            .AsNoTracking()
            .Where(x => x.Status == NewsletterIssueStatuses.Sending)
            .OrderBy(x => x.Id)
            .Take(5)
            .ToQueryString();

        sql.Should().Contain("[NewsletterIssues]");
        sql.Should().Contain("IsDeleted", "küresel süzgeç sorguya girmeli; pasife alınan sayı dağıtımdan çıkar");
    }

    [Fact]
    public void Bekleyen_dagitim_satirlarinin_imlecli_sayfasi_cevrilebilir()
    {
        using var db = Context();
        const int issueId = 3;
        const int cursor = 25;

        var sql = db.NewsletterDeliveries
            .AsNoTracking()
            .Where(d => d.NewsletterIssueId == issueId
                        && d.Status == NewsletterDeliveryStatuses.Pending
                        && d.AttemptCount < 3
                        && d.Id > cursor)
            .OrderBy(d => d.Id)
            .Take(25)
            .ToQueryString();

        sql.Should().Contain("[NewsletterDeliveries]");
        sql.Should().Contain("ORDER BY", "imleç kimlik sırasına dayanır; sıralama düşerse tur satır atlar");
    }

    [Fact]
    public void Toplu_abone_okumasi_cevrilebilir()
    {
        using var db = Context();
        var ids = new List<int> { 1, 2, 3 };

        var sql = db.Subscribers
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id) && s.ConfirmedAt != null)
            .ToQueryString();

        sql.Should().Contain("[Subscribers]");
    }

    [Fact]
    public void Alici_projeksiyonu_cevrilebilir_ve_govde_sutunlarini_okumaz()
    {
        using var db = Context();

        var sql = db.Subscribers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.IsActive && s.ConfirmedAt != null && s.Email != null)
            .OrderBy(s => s.Id)
            .Select(s => new Subscriber { Id = s.Id, Email = s.Email })
            .Take(500)
            .ToQueryString();

        sql.Should().Contain("[Email]");
        sql.Should().NotContain("[ConfirmedAt],", "projeksiyon yalnızca kimlik ve adresi taşımalı");
    }

    [Fact]
    public void Dagitim_sayaci_sorgusu_cevrilebilir()
    {
        using var db = Context();
        const int issueId = 3;

        var sql = db.NewsletterDeliveries
            .AsNoTracking()
            .Where(d => d.NewsletterIssueId == issueId && d.Status == NewsletterDeliveryStatuses.Sent)
            .ToQueryString();

        sql.Should().Contain("[NewsletterDeliveries]");
    }

    [Fact]
    public void Cikis_jetonu_sogumasi_omur_penceresiyle_cevrilebilir()
    {
        using var db = Context();
        var cutoff = new DateTime(2026, 9, 7, 9, 55, 0, DateTimeKind.Utc);
        var horizon = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);

        var sql = db.SubscriberVerifications
            .AsNoTracking()
            .Where(x => x.SubscriberId == 7
                        && x.Purpose == SubscriberVerificationPurposes.Unsubscribe
                        && x.ConsumedAt == null
                        && x.CreatedAt > cutoff
                        && x.ExpiresAt <= horizon)
            .ToQueryString();

        sql.Should().Contain("[SubscriberVerifications]");
        sql.Should().Contain("[ExpiresAt]", "ömür penceresi sorguya girmeli; bülten jetonları soğumayı bloklamamalı");
    }
}
