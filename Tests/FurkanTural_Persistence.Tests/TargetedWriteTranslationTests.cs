using System.Data.Common;
using FluentAssertions;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FurkanTural_Persistence.Tests;

public class TargetedWriteTranslationTests
{
    private sealed class OfflineConnection : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
            => InterceptionResult.Suppress();

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class CommandCapture : DbCommandInterceptor
    {
        public List<string> Statements { get; } = [];

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Statements.Add(command.CommandText);
            return InterceptionResult<int>.SuppressWithResult(1);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Statements.Add(command.CommandText);
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
        }
    }

    private static FurkanTuralDbContext Context(CommandCapture capture)
        => new(new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .AddInterceptors(new OfflineConnection(), capture)
            .Options);

    private static string SetClause(string sql)
    {
        var start = sql.IndexOf("SET", StringComparison.Ordinal);
        var end = sql.IndexOf("FROM", StringComparison.Ordinal);
        return start >= 0 && end > start ? sql[start..end] : string.Empty;
    }

    [Fact]
    public async Task Son_gorulme_yalnizca_kendi_sutununu_ve_guncelleme_damgasini_yazar()
    {
        var capture = new CommandCapture();
        await using var db = Context(capture);

        var touched = await new UserRepository(db).TouchLastSeenAsync(7, new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc));

        touched.Should().BeTrue();
        var sql = capture.Statements.Should().ContainSingle("tek kullanıcı için tek bir UPDATE yeter").Subject;
        var set = SetClause(sql);

        sql.Should().StartWith("UPDATE");
        set.Should().Contain("[LastSeenAt]").And.Contain("[UpdatedAt]");
        set.Should().NotContain("[Username]", "yazma satırın geri kalanına dokunursa aynı anda yapılan yönetici değişikliğini ezer");
        set.Should().NotContain("[IsActive]", "pasife alınmış kullanıcı son görülme yazımıyla yeniden aktifleşmemeli");
        sql.Should().Contain("[IsDeleted]", "silinmiş ya da pasif kullanıcıya bugünkü canlı satır okumasında olduğu gibi dokunulmaz");
        db.ChangeTracker.Entries().Should().BeEmpty("hedefli yazma kaydetme yoluna uğramaz; canlı bildirim kancası bu yüzden onu hiç görmez");
    }

    [Fact]
    public async Task Okundu_isareti_icerige_dokunmadan_yalnizca_okunma_alanlarini_yazar()
    {
        var capture = new CommandCapture();
        await using var db = Context(capture);

        await new ChatMessageRepository(db).MarkConversationReadAsync(9, 3, new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc));

        var sql = capture.Statements.Should().ContainSingle("bir konuşmanın bütün okunmamış mesajları tek UPDATE ile işaretlenir").Subject;
        var set = SetClause(sql);

        sql.Should().StartWith("UPDATE");
        set.Should().Contain("[IsRead]").And.Contain("[ReadAt]").And.Contain("[UpdatedAt]");
        set.Should().NotContain("[Content]", "şifreli içerik yeniden yazılırsa aynı anda yapılan düzenleme kaybolur");
        sql.Should().Contain("[SenderId]").And.Contain("[ReceiverId]");
        db.ChangeTracker.Entries().Should().BeEmpty("okundu işareti kaydetme yoluna uğramaz; canlı bildirim kancası bu yüzden onu hiç görmez");
    }

    [Fact]
    public async Task Tek_kullanimlik_jeton_yalnizca_harcanmamisken_isaretlenir()
    {
        var capture = new CommandCapture();
        await using var db = Context(capture);

        var consumed = await new UnitOfWork(db)
            .TryConsumeTokenAsync<AccountActivation>(5, new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc));

        consumed.Should().BeTrue("tek satır etkilendiyse jetonu harcayan bu çağrıdır");
        var sql = capture.Statements.Should().ContainSingle("harcama tek bir UPDATE ile yapılır").Subject;
        var set = SetClause(sql);

        sql.Should().StartWith("UPDATE");
        set.Should().Contain("[ConsumedAt]").And.Contain("[UpdatedAt]");
        set.Should().NotContain("[TokenHash]", "jetonun özetine dokunulmaz; yazma yalnızca harcanma anını taşır");
        sql.Should().Contain("IS NULL",
            "harcanmışlık koşulu WHERE'e girmeli — okuma ile yazma arasındaki boşluğu kapatan tek şey bu koşuldur");
        sql.Should().Contain("[IsDeleted]", "silinmiş jeton satırı canlı okumada olduğu gibi harcanamaz");
        db.ChangeTracker.Entries().Should().BeEmpty("hedefli yazma kaydetme yoluna uğramaz");
    }

    [Fact]
    public async Task Abonenin_bekleyen_baglantilari_tek_yazmayla_harcanir()
    {
        var capture = new CommandCapture();
        await using var db = Context(capture);

        await new UnitOfWork(db).ConsumePendingSubscriberVerificationsAsync(7, new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc));

        var sql = capture.Statements.Should().ContainSingle().Subject;
        var set = SetClause(sql);

        sql.Should().StartWith("UPDATE");
        sql.Should().Contain("[SubscriberVerifications]");
        set.Should().Contain("[ConsumedAt]").And.Contain("[UpdatedAt]");
        sql.Should().Contain("[SubscriberId]").And.Contain("IS NULL",
            "yalnızca bu abonenin henüz harcanmamış bağlantıları işaretlenmeli; harcanmış olanların anı değişmemeli");
    }
}
