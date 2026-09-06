using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Slug tekildir ve süzgeçsizdir: silinmiş bir etiketin adresi başkasına verilemez, aksi hâlde geri yükleme çakışırdı. Aynı gerekçe <see cref="CategoryConfiguration"/> için de geçerlidir.<para>Ad da tekildir. Kategoride böyle bir kısıt yok ama etiket sayıca çok daha kalabalık olacağı için aynı adın iki kez açılması kaçınılmazdır; kuralı veri tabanına koymak, panelde iki "EF Core" etiketinin yan yana durmasını yazma anında engeller.</para></summary>
public class TagConfiguration : BaseEntityConfiguration<Tag>
{
    public override void Configure(EntityTypeBuilder<Tag> builder)
    {
        base.Configure(builder);
        builder.ToTable("Tags");

        builder.Property(e => e.Name).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(120).IsRequired();

        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.Name).IsUnique();
    }
}
