using System.Data.Common;
using FluentAssertions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace FurkanTural_Persistence.Tests;

public class AdminChangeInterceptorTests
{
    private sealed class RecordingFeed : IAdminChangeFeed
    {
        public List<IReadOnlyList<AdminEntityChange>> Published { get; } = [];

        public void Publish(IReadOnlyList<AdminEntityChange> changes) => Published.Add(changes);
    }

    private sealed class ThrowingFeed : IAdminChangeFeed
    {
        public void Publish(IReadOnlyList<AdminEntityChange> changes) => throw new InvalidOperationException("yayın düştü");
    }

    private sealed class SuppressSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
    }

    private sealed class ThrowOnceBeforeSave : SaveChangesInterceptor
    {
        private bool _thrown;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (_thrown)
                return base.SavingChangesAsync(eventData, result, cancellationToken);

            _thrown = true;
            throw new InvalidOperationException("sonraki kanca düştü");
        }
    }

    private sealed class FailOnOpen : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("veri tabanına ulaşılamadı");
    }

    private static AdminChangeInterceptor Interceptor(params IAdminChangeFeed[] feeds)
        => new(feeds, NullLogger<AdminChangeInterceptor>.Instance);

    private static FurkanTuralDbContext Context(params IInterceptor[] interceptors)
        => new(new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .AddInterceptors(interceptors)
            .Options);

    [Fact]
    public async Task Basarili_kaydetmeden_sonra_degisiklikler_yayinlanir()
    {
        var feed = new RecordingFeed();
        await using var db = Context(new SuppressSave(), Interceptor(feed));
        db.Add(new Comment());
        db.Add(new Comment());
        db.Entry(new User { Id = 5 }).State = EntityState.Modified;

        await db.SaveChangesAsync();

        feed.Published.Should().ContainSingle("tek kaydetme tek yayın üretir").Which.Should().BeEquivalentTo(new[]
        {
            new AdminEntityChange(AdminListKinds.Comment, 2, 0),
            new AdminEntityChange(AdminListKinds.User, 0, 1)
        });
    }

    [Fact]
    public async Task Kaydetme_basarisizsa_hicbir_sey_yayinlanmaz()
    {
        var feed = new RecordingFeed();
        await using var db = Context(new FailOnOpen(), Interceptor(feed));
        db.Add(new Comment());

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>();
        feed.Published.Should().BeEmpty("yazılmamış bir kayıt için panele haber gitmemeli; yönetici listede olmayan bir satırı arar");
    }

    [Fact]
    public async Task Izlenmeyen_tablo_yayin_uretmez()
    {
        var feed = new RecordingFeed();
        await using var db = Context(new SuppressSave(), Interceptor(feed));
        db.Add(new Blog());
        db.Add(new Log());

        await db.SaveChangesAsync();

        feed.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task Yarim_kalan_kaydetmenin_degisiklikleri_sonraki_kaydetmeyle_yayinlanmaz()
    {
        var feed = new RecordingFeed();
        await using var db = Context(new SuppressSave(), Interceptor(feed), new ThrowOnceBeforeSave());
        var comment = new Comment();
        db.Add(comment);

        var first = () => db.SaveChangesAsync();
        await first.Should().ThrowAsync<InvalidOperationException>();

        db.Entry(comment).State = EntityState.Detached;
        db.Add(new Blog());
        await db.SaveChangesAsync();

        feed.Published.Should().BeEmpty(
            "yarım kalan kaydetmenin yakaladığı değişiklik bekletilip bir sonraki kaydetmede yayınlanırsa panel hiç yazılmamış bir kaydı duyurur");
    }

    [Fact]
    public async Task Dinleyicinin_hatasi_kaydetmeyi_bozmaz_ve_digerlerine_ulasir()
    {
        var feed = new RecordingFeed();
        await using var db = Context(new SuppressSave(), Interceptor(new ThrowingFeed(), feed));
        db.Add(new Report());

        var saved = await db.SaveChangesAsync();

        saved.Should().Be(1, "satır yazılmıştır; yayın hatası isteği başarısız gösterirse istemci kaydı yeniden gönderir");
        feed.Published.Should().ContainSingle("bir dinleyicinin düşmesi ötekilerin haberini kesmemeli");
    }
}
