using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class ProjectConfiguration : BaseEntityConfiguration<Project>
{
    public override void Configure(EntityTypeBuilder<Project> builder)
    {
        base.Configure(builder);
        builder.ToTable("Projects");
        builder.Property(e => e.Title).HasMaxLength(500);
        builder.Property(e => e.Description).HasColumnType("nvarchar(max)");
        builder.Property(e => e.ShortDescription).HasMaxLength(300);
        builder.Property(e => e.TechStack).HasMaxLength(500);
        builder.Property(e => e.GitHubUrl).HasMaxLength(1000);
        builder.Property(e => e.DemoUrl).HasMaxLength(1000);
        builder.Property(e => e.IsCompleted).HasDefaultValue(false);
    }
}
