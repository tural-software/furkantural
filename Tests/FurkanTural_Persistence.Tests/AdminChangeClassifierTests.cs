using FluentAssertions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using FurkanTural_Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Tests;

public class AdminChangeClassifierTests
{
    private static readonly Type[] AgreedTypes =
    [
        typeof(Comment), typeof(Contact), typeof(Report), typeof(User), typeof(UserFriend),
        typeof(ChatMessage), typeof(CallLog), typeof(Subscriber), typeof(NewsletterIssue)
    ];

    private static FurkanTuralDbContext Context()
        => new(new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer("Server=yok;Database=yok;Trusted_Connection=True;")
            .Options);

    [Fact]
    public void Izlenen_kume_kararlastirilan_dokuz_turdur()
    {
        AdminChangeClassifier.Watched.Keys.Should().BeEquivalentTo(AgreedTypes,
            "Blog, Log, bülten teslimatları, doğrulama jetonları ve yöneticiye ait içerik tabloları haber üretmez; kapsam bilinçli olarak dokuz tabloyla sınırlı");
        AdminChangeClassifier.Watched.Values.Should().BeEquivalentTo(AdminListKinds.All,
            "tür anahtarları istemcinin tanıdığı liste türleriyle birebir aynı olmalı");
    }

    [Fact]
    public void Eklenen_ile_degisen_ayri_sayilir_ve_ture_gore_toplanir()
    {
        using var db = Context();
        db.Add(new Comment());
        db.Add(new Comment());
        db.Entry(new Comment { Id = 3 }).State = EntityState.Modified;
        db.Entry(new User { Id = 5 }).State = EntityState.Deleted;
        db.Entry(new ChatMessage { Id = 8, IsDeleted = true }).State = EntityState.Modified;

        var changes = AdminChangeClassifier.Classify(db.ChangeTracker.Entries());

        changes.Should().BeEquivalentTo(new[]
        {
            new AdminEntityChange(AdminListKinds.Comment, 2, 1),
            new AdminEntityChange(AdminListKinds.User, 0, 1),
            new AdminEntityChange(AdminListKinds.Message, 0, 1)
        }, "şerit yeni kaydı değişen kayıttan ayırır; yumuşak silme ve kalıcı silme değişiklik sayılır");
    }

    [Fact]
    public void Degismeyen_ve_izlenmeyen_kayitlar_haber_uretmez()
    {
        using var db = Context();
        db.Entry(new Contact { Id = 1 }).State = EntityState.Unchanged;
        db.Add(new Blog());
        db.Add(new Log());
        db.Entry(new Category { Id = 2 }).State = EntityState.Modified;

        AdminChangeClassifier.Classify(db.ChangeTracker.Entries()).Should().BeEmpty(
            "yalnızca okunmuş kayıt ve yöneticiye ait ya da kapsam dışı tablolar paneli meşgul etmemeli");
    }
}
