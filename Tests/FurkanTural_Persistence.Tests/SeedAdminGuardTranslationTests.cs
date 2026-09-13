using System.Data.Common;
using FluentAssertions;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FurkanTural_Persistence.Tests;

public class SeedAdminGuardTranslationTests
{
    private sealed class OfflineConnection : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
            => InterceptionResult.Suppress();

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class QueryCaptured : Exception
    {
    }

    private sealed class StopAtQuery : DbCommandInterceptor
    {
        public string? Sql { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Sql = command.CommandText;
            throw new QueryCaptured();
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql = command.CommandText;
            throw new QueryCaptured();
        }
    }

    private static FurkanTuralDbContext Context(StopAtQuery capture)
        => new(new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .AddInterceptors(new OfflineConnection(), capture)
            .Options);

    [Fact]
    public async Task Ilk_yonetici_korumasinin_sayimi_pasif_ve_silinmis_kullanicilari_da_gorur()
    {
        var capture = new StopAtQuery();
        await using var db = Context(capture);

        var act = () => new UserRepository(db).CountForAdminAsync();

        await act.Should().ThrowAsync<Exception>();
        capture.Sql.Should().NotBeNull().And.Contain("COUNT");
        capture.Sql.Should()
            .NotContain("[IsActive]", "pasife alınmış tek yönetici varken uç anonim bir yönetici açmamalı")
            .And.NotContain("[IsDeleted]", "silinmiş kullanıcılar da sistemin kurulmuş olduğunu gösterir");
    }

    [Fact]
    public async Task Suzgecli_varlik_kontrolu_pasif_kullaniciyi_gormez()
    {
        var capture = new StopAtQuery();
        await using var db = Context(capture);

        var act = () => new UserRepository(db).AnyAsync(_ => true);

        await act.Should().ThrowAsync<Exception>();
        capture.Sql.Should().NotBeNull().And.Contain("[IsActive]",
            "bu okuma küresel süzgeçten geçer; ilk yönetici korumasında kullanılırsa kurulu bir sistemi boş sanar");
    }
}
