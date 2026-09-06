using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Durum indeksi dağıtıcı içindir: her turda "gönderime verilmiş sayı var mı" sorusu sorulur ve bu soru uygulama ayakta durduğu sürece dakikada bir tekrarlanır, dolayısıyla tablo taraması olmamalıdır.<para>Gövde <c>nvarchar(max)</c>'tır ve sınırlanmaz; bülten metninin uzunluğuna teknik bir tavan koymak, yazının kendisine keyfî bir sınır koymak olurdu.</para></summary>
public class NewsletterIssueConfiguration : BaseEntityConfiguration<NewsletterIssue>
{
    public override void Configure(EntityTypeBuilder<NewsletterIssue> builder)
    {
        base.Configure(builder);
        builder.ToTable("NewsletterIssues");

        builder.Property(e => e.Subject).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Body).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.Status).HasMaxLength(NewsletterIssueStatuses.MaxLength).IsRequired();

        builder.HasIndex(e => e.Status);
    }
}
