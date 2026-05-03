using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class BlogImageConfiguration : BaseEntityConfiguration<BlogImage>
{
    public override void Configure(EntityTypeBuilder<BlogImage> builder)
    {
        base.Configure(builder);
        builder.ToTable("BlogImages");
        builder.Property(e => e.Url).HasMaxLength(1000);
        builder.Property(e => e.AltText).HasMaxLength(500);
        builder.HasOne<Blog>()
            .WithMany()
            .HasForeignKey(e => e.BlogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}