using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class ExperienceConfiguration : BaseEntityConfiguration<Experience>
{
    public override void Configure(EntityTypeBuilder<Experience> builder)
    {
        base.Configure(builder);
        builder.ToTable("Experiences");
        builder.Property(e => e.Position).HasMaxLength(200);
        builder.Property(e => e.CompanyName).HasMaxLength(200);
    }
}
