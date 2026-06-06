using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.ToTable("Users");
        builder.Property(e => e.Username).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Password).HasMaxLength(500).IsRequired();
        builder.HasIndex(e => e.Username).IsUnique();

        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.DisplayName).HasMaxLength(150);
        builder.Property(e => e.AvatarUrl).HasMaxLength(500);
        builder.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}