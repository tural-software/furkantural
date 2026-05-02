using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class BlogConfiguration : BaseEntityConfiguration<Blog>
{
    public override void Configure(EntityTypeBuilder<Blog> builder)
    {
        base.Configure(builder);
        builder.ToTable("Blogs");
        builder.Property(e => e.Title).HasMaxLength(500);
        builder.Property(e => e.Content).HasColumnType("nvarchar(max)");
    }
}
