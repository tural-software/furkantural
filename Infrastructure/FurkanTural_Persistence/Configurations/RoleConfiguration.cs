using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class RoleConfiguration : BaseEntityConfiguration<Role>
{
    private static readonly DateTime SeedDate = new(2026, 5, 9, 0, 0, 0, DateTimeKind.Utc);

    public override void Configure(EntityTypeBuilder<Role> builder)
    {
        base.Configure(builder);
        builder.ToTable("Roles");
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Name).IsUnique();

        builder.HasData(
            new Role
            {
                Id = 1,
                Name = "Admin",
                CreatedAt = SeedDate,
                IsActive = true,
                IsDeleted = false
            },
            new Role
            {
                Id = 2,
                Name = "User",
                CreatedAt = SeedDate,
                IsActive = true,
                IsDeleted = false
            },
            new Role
            {
                Id = 3,
                Name = "Subscriber",
                CreatedAt = SeedDate,
                IsActive = true,
                IsDeleted = false
            },
            new Role
            {
                Id = 4,
                Name = "Visitor",
                CreatedAt = SeedDate,
                IsActive = true,
                IsDeleted = false
            }
        );
    }
}
