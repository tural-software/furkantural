using FluentAssertions;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Tests;

/// <summary>Saklama temizliğinin sorguları gerçekten SQL'e çevrilebiliyor mu ve global süzgeci atlıyor mu. Süzgeç silinmiş ve pasif satırları gizler; temizliğin silmesi gereken satırların büyük kısmı tam olarak bunlardır. Süzgeç sorguya girerse temizlik sessizce hiçbir şey silmez.</summary>
public class RetentionQueryTranslationTests
{
    private static readonly DateTime Cutoff = new(2024, 10, 16, 0, 0, 0, DateTimeKind.Utc);

    private static FurkanTuralDbContext Context()
        => new(new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .Options);

    public static TheoryData<string> Queries => new()
    {
        "users", "logs", "contacts", "activations", "calls", "messages", "reports", "friendships",
        "subscribers", "subscriberVerifications", "newsletterDeliveries", "comments", "commentNotifications", "push"
    };

    private static string Sql(FurkanTuralDbContext db, string name) => name switch
    {
        "users" => RetentionQueries.ClosedUsers(db, Cutoff).ToQueryString(),
        "logs" => RetentionQueries.Logs(db, Cutoff).ToQueryString(),
        "contacts" => RetentionQueries.Contacts(db, Cutoff).ToQueryString(),
        "activations" => RetentionQueries.AccountActivations(db, Cutoff).ToQueryString(),
        "calls" => RetentionQueries.CallLogs(db, Cutoff).ToQueryString(),
        "messages" => RetentionQueries.ChatMessages(db, Cutoff).ToQueryString(),
        "reports" => RetentionQueries.Reports(db, Cutoff).ToQueryString(),
        "friendships" => RetentionQueries.UserFriends(db, Cutoff).ToQueryString(),
        "subscribers" => RetentionQueries.Subscribers(db, Cutoff).ToQueryString(),
        "subscriberVerifications" => RetentionQueries.SubscriberVerifications(db, Cutoff).ToQueryString(),
        "newsletterDeliveries" => RetentionQueries.NewsletterDeliveries(db, Cutoff).ToQueryString(),
        "comments" => RetentionQueries.CommentCandidates(db, Cutoff).ToQueryString(),
        "commentNotifications" => RetentionQueries.CommentNotifications(db, Cutoff, [1, 2]).ToQueryString(),
        "push" => RetentionQueries.StalePushSubscriptions(db, Cutoff).ToQueryString(),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    [Theory]
    [MemberData(nameof(Queries))]
    public void Sorgu_cevrilebilir_ve_global_suzgeci_atlar(string name)
    {
        using var db = Context();

        var sql = Sql(db, name);

        sql.Should().StartWith("DECLARE").And.Contain("SELECT");
        sql.Should().NotContain("[IsActive] = CAST(1 AS bit) AND", "global süzgeç silinmiş ve pasif satırları gizler; temizlik onları görmeli");
    }

    [Fact]
    public void Yonetici_hesaplari_kapali_olsa_da_silinmez()
    {
        using var db = Context();

        var sql = RetentionQueries.ClosedUsers(db, Cutoff).ToQueryString();

        sql.Should().Contain("[Roles]").And.Contain("N'Admin'",
            "panelin tek yönetici hesabının silinmesi geri alınamaz bir erişim kaybıdır");
        sql.Should().Contain("[DeactivatedAt]").And.Contain("[DeletedAt]",
            "süre hesabın kapatıldığı ya da silindiği andan işler, oluşturulduğu andan değil");
    }

    [Fact]
    public void Suren_aboneligin_dogrulama_kayitlari_silinmez()
    {
        using var db = Context();

        var sql = RetentionQueries.SubscriberVerifications(db, Cutoff).ToQueryString();

        sql.Should().Contain("[Subscribers]",
            "doğrulama kaydı izinli pazarlamanın kanıtıdır; yalnızca çıkmış ya da hiç doğrulanmamış abonelerinki silinir");
        sql.Should().NotContain("[s].[CreatedAt] < @",
            "doğrulama kaydının kendi yaşı silme sebebi değildir");
    }
}
