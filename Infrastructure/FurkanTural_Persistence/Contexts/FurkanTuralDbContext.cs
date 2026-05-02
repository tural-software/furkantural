using FurkanTural_Domain.Entities;
using FurkanTural_Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Contexts;

public class FurkanTuralDbContext : DbContext
{
    public FurkanTuralDbContext(DbContextOptions<FurkanTuralDbContext> options) : base(options) { }

    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<BlogImage> BlogImages => Set<BlogImage>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<Music> Musics => Set<Music>();
    public DbSet<MusicImage> MusicImages => Set<MusicImage>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<User> Users => Set<User>();

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
