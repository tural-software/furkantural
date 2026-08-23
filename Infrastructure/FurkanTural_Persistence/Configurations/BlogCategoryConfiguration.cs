using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Blog ile kategori arasındaki bağ tablosu. Silme davranışı iki yönde farklıdır: blog satırı gerçekten silinirse bağlar da gider, kategori ise bağlıyken silinemez. Bağ satırları yumuşak silinmez, servis katmanı onları gerçekten siler.</summary>
public class BlogCategoryConfiguration : BaseEntityConfiguration<BlogCategory>
{
    public override void Configure(EntityTypeBuilder<BlogCategory> builder)
    {
        base.Configure(builder);
        builder.ToTable("BlogCategories");

        builder.HasOne<Blog>()
            .WithMany()
            .HasForeignKey(e => e.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.BlogId, e.CategoryId });
    }
}
