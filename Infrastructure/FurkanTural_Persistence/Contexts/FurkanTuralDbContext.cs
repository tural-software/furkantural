using FurkanTural_Domain.Entities.Common;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FurkanTuralDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.IsActive = true;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}