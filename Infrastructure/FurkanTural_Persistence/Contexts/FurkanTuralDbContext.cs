using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FurkanTural_Persistence.Contexts;

public class FurkanTuralDbContext(DbContextOptions<FurkanTuralDbContext> options) : DbContext(options)
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<BlogImage> BlogImages => Set<BlogImage>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<Music> Musics => Set<Music>();
    public DbSet<MusicImage> MusicImages => Set<MusicImage>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactTemplate> ContactTemplates => Set<ContactTemplate>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<UserFriend> UserFriends => Set<UserFriend>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<CallLog> CallLogs => Set<CallLog>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<CallPolicy> CallPolicies => Set<CallPolicy>();

    // Tüm DateTime'ları DB'den UTC olarak materyalize et (Kind=Utc) → System.Text.Json daima 'Z' ile yazar.
    // Yazarken değer korunur (kanonik kural: her zaman UTC saklanır).
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter =
        new(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FurkanTuralDbContext).Assembly);

        // Audit zaman damgaları (CreatedAt/UpdatedAt/DeletedAt) AuditSaveChangesInterceptor'da set edilir.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.GetValueConverter() is not null) continue; // mevcut converter'ı ezme
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(UtcConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(NullableUtcConverter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}