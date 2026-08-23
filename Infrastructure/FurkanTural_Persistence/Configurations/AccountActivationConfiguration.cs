using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>TokenHash indeksi bilerek tekil değildir. Jeton kriptografik rastgeleden üretildiği için çarpışma pratikte imkânsız, buna karşılık yumuşak silmeye göre süzülmeyen bir tekil indeks <see cref="UserConfiguration"/>'daki tuzağın aynısını kurardı: orada silinmiş satırın kullanıcı adı tutulu kaldığı için kayıt akışı tekil indeks ihlaline düşüyordu.<para>User bağı Restrict'tir, <see cref="PushSubscriptionConfiguration"/>'daki Cascade değil — aktivasyon kayıtlarının hesapla birlikte kaybolmaması istenir. Kullanıcı yumuşak silindiği için bu seçim yalnızca elle yapılacak kalıcı bir silmede fark eder.</para><para>Taban süzgeç burada da geçerlidir: satır yalnızca IsDeleted false ve IsActive true iken okunur. Tüketim IsActive'e dokunmaz, ConsumedAt'e yazar; aksi hâlde tüketilmiş satır sorgulardan tümüyle kaybolur ve tekrar kullanım denemesi "jeton yok" ile "jeton harcanmış" arasındaki farkı yitirirdi.</para></summary>
public class AccountActivationConfiguration : BaseEntityConfiguration<AccountActivation>
{
    public override void Configure(EntityTypeBuilder<AccountActivation> builder)
    {
        base.Configure(builder);
        builder.ToTable("AccountActivations");

        builder.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.RequestIpAddress).HasMaxLength(45);
        builder.Property(e => e.RequestUserAgent).HasMaxLength(300);
        builder.Property(e => e.Trigger).HasMaxLength(50);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.TokenHash);
        builder.HasIndex(e => e.UserId);
    }
}
