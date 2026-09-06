using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Sayı ve abone çifti tekildir. Kural veri tabanındadır çünkü asıl koruduğu şey uygulamanın iyi niyeti değil sonucudur: aynı aboneye aynı sayıdan iki satır açılırsa kişi bülteni iki kez alır ve bunu geri almanın yolu yoktur. İndeks yumuşak silmeye göre süzülmez — silinmiş bir dağıtım satırı da "bu kişiye gönderildi" bilgisini taşır.<para>İkinci indeks dağıtıcının tek sorgusudur: bir sayının bekleyen satırlarını kimlik sırasıyla ister. Bekleyen satırlar tükendikçe indeksin taranan bölümü de küçülür.</para><para>Her iki bağ da Restrict'tir. Gönderim kaydı, sayı ya da abone kaydı ortadan kalksa bile durmalıdır: bir yıl sonra "bu bülten kime gitti" sorusunun cevabı yalnızca burada yazılıdır.</para></summary>
public class NewsletterDeliveryConfiguration : BaseEntityConfiguration<NewsletterDelivery>
{
    public override void Configure(EntityTypeBuilder<NewsletterDelivery> builder)
    {
        base.Configure(builder);
        builder.ToTable("NewsletterDeliveries");

        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(NewsletterDeliveryStatuses.MaxLength).IsRequired();
        builder.Property(e => e.Error).HasMaxLength(500);

        builder.HasOne<NewsletterIssue>()
            .WithMany()
            .HasForeignKey(e => e.NewsletterIssueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subscriber>()
            .WithMany()
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.NewsletterIssueId, e.SubscriberId }).IsUnique();
        builder.HasIndex(e => new { e.NewsletterIssueId, e.Status });
    }
}
