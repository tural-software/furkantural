using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Blog ile etiket arasındaki bağ tablosu; silme davranışı <see cref="BlogCategoryConfiguration"/> ile aynıdır — blog satırı gerçekten silinirse bağlar da gider, etiket bağlıyken silinemez.<para>Kategori bağından tek farkı indeksin tekil olmasıdır. Aynı yazıya aynı etiketi iki kez bağlamanın hiçbir anlamı yok ve tekrar, yazı sayfasında aynı çipin iki kez çizilmesi demek; kuralı burada tutmak servisin iyi niyetine güvenmekten sağlamdır.</para></summary>
public class BlogTagConfiguration : BaseEntityConfiguration<BlogTag>
{
    public override void Configure(EntityTypeBuilder<BlogTag> builder)
    {
        base.Configure(builder);
        builder.ToTable("BlogTags");

        builder.HasOne<Blog>()
            .WithMany()
            .HasForeignKey(e => e.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.BlogId, e.TagId }).IsUnique();
    }
}
