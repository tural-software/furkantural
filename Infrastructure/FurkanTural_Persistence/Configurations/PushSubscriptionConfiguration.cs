using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>
/// Endpoint üzerindeki indeks tekil değildir, oysa abonelik akışı endpoint'i cihazın kimliği gibi
/// kullanıp tek satır bekler. Aynı endpoint için eş zamanlı gelen iki kayıt isteği veri tabanınca
/// engellenmez; böyle bir durumda okuma satırlardan yalnızca birini görür.
/// </summary>
public class PushSubscriptionConfiguration : BaseEntityConfiguration<PushSubscription>
{
    public override void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        base.Configure(builder);
        builder.ToTable("PushSubscriptions");

        builder.Property(e => e.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(e => e.P256dh).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Auth).HasMaxLength(100).IsRequired();
        builder.Property(e => e.UserAgent).HasMaxLength(300);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Endpoint);
    }
}