using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class CategoryConfiguration : BaseEntityConfiguration<Category>
{
    public override void Configure(EntityTypeBuilder<Category> builder)
    {
        base.Configure(builder);
        builder.ToTable("Categories");
        builder.Property(e => e.Name).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Color).HasMaxLength(20);
    }
}
