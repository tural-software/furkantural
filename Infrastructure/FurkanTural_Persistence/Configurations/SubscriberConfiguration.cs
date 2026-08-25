using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>E-posta tekil indeksi yumuşak silmeye göre süzülmez ve öyle kalması doğrudur: filtre eklemek, abonelikten çıkmış bir adresin ikinci bir satırla yeniden kaydedilebilmesi demek olurdu.<para>Bunun bedeli varlık kontrolünün indeksle aynı şeyi görmek zorunda olmasıdır; abonelik akışı bu yüzden süzgeçsiz okur ve duran satırı geri açar (bkz. <see cref="FurkanTural_Application.Repositories.Abstract.ISubscriberRepository"/>). Aynı durum <see cref="UserConfiguration"/> için de geçerlidir.</para></summary>
public class SubscriberConfiguration : BaseEntityConfiguration<Subscriber>
{
    public override void Configure(EntityTypeBuilder<Subscriber> builder)
    {
        base.Configure(builder);
        builder.ToTable("Subscribers");
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.HasIndex(e => e.Email).IsUnique();
    }
}
