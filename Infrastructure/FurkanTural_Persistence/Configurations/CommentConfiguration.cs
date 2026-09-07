using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Yorum tablosu. Yazı bağı Cascade'dir: bir blog satırı gerçekten silinirse — yumuşak silme değil, tablodan kalkması — altındaki yorumların sahipsiz kalmasının bir anlamı olmaz.<para>Üst yorum bağı Restrict'tir ve bilinçli olarak farklıdır: yanıtı olan bir yorumun tablodan kaldırılmasına izin vermek, yanıtı bağlamsız bırakırdı. Yönetici bir yorumu yayından kaldırmak istediğinde zaten silmez, reddeder ya da pasife alır.</para><para>İki indeks iki farklı ekran içindir. İlki okurun gördüğü sorgudur — bir yazının onaylı yorumları — ikincisi yöneticinin ilk açtığı ekrandır, yani bekleyenler. İkisi ayrı tutulur çünkü biri yazıya göre daralır, diğeri bütün yazıları birden tarar.</para></summary>
public class CommentConfiguration : BaseEntityConfiguration<Comment>
{
    public override void Configure(EntityTypeBuilder<Comment> builder)
    {
        base.Configure(builder);
        builder.ToTable("Comments");

        builder.Property(e => e.AuthorName).HasMaxLength(80).IsRequired();
        builder.Property(e => e.AuthorEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(CommentStatuses.MaxLength).IsRequired();

        builder.HasOne<Blog>()
            .WithMany()
            .HasForeignKey(e => e.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.BlogId, e.Status });
        builder.HasIndex(e => e.Status);
    }
}
