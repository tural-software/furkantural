using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>
/// E-posta tekil indeksi yumuşak silmeye göre süzülmez. Abonelikten çıkmak kaydı yumuşak sildiği için
/// satır tabloda kalır; aynı adresle yeniden abone olma girişimi ise varlık kontrolünü global süzgeç
/// yüzünden geçer ve ekleme tekil indekse takılır. Aynı durum <see cref="UserConfiguration"/> için de
/// geçerlidir.
/// </summary>
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