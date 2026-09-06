using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>TokenHash indeksi <see cref="AccountActivationConfiguration"/>'daki gerekçeyle tekil değildir: jeton kriptografik rastgeleden üretildiği için çarpışma pratikte imkânsız, buna karşılık yumuşak silmeye göre süzülmeyen bir tekil indeks silinmiş satırların özetini tutulu bırakırdı.<para>Subscriber bağı Restrict'tir: doğrulama kayıtları aboneyle birlikte kaybolmamalı. Bir adresin listeye ne zaman ve nereden girdiğinin izi, izinli pazarlamanın kendisini kanıtlayan şeydir; abone kaydı silinse de o iz durur.</para><para>Taban süzgeç burada da geçerlidir. Tüketim IsActive'e dokunmaz, ConsumedAt'e yazar; aksi hâlde harcanmış satır sorgulardan tümüyle kaybolur ve tekrar kullanım denemesi "jeton yok" ile "jeton harcanmış" arasındaki farkı yitirirdi.</para></summary>
public class SubscriberVerificationConfiguration : BaseEntityConfiguration<SubscriberVerification>
{
    public override void Configure(EntityTypeBuilder<SubscriberVerification> builder)
    {
        base.Configure(builder);
        builder.ToTable("SubscriberVerifications");

        builder.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Purpose).HasMaxLength(SubscriberVerificationPurposes.MaxLength).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.RequestIpAddress).HasMaxLength(45);
        builder.Property(e => e.RequestUserAgent).HasMaxLength(300);

        builder.HasOne<Subscriber>()
            .WithMany()
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.TokenHash);
        builder.HasIndex(e => e.SubscriberId);
    }
}
