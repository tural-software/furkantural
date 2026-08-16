using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class ReportConfiguration : BaseEntityConfiguration<Report>
{
    public override void Configure(EntityTypeBuilder<Report> builder)
    {
        base.Configure(builder);
        builder.ToTable("Reports");

        builder.Property(e => e.TargetType).HasMaxLength(20);
        builder.Property(e => e.Status).HasMaxLength(20);
        builder.Property(e => e.Reason).HasMaxLength(1000);
        builder.Property(e => e.AdminNote).HasMaxLength(1000);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Status);
    }
}