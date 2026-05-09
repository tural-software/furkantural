using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class ProjectImageConfiguration : BaseEntityConfiguration<ProjectImage>
{
    public override void Configure(EntityTypeBuilder<ProjectImage> builder)
    {
        base.Configure(builder);
        builder.ToTable("ProjectImages");
        builder.Property(e => e.Url).HasMaxLength(1000);
        builder.Property(e => e.AltText).HasMaxLength(500);
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
