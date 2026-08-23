using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Tekil indeks yalnızca silinmemiş satırları kapsar, dolayısıyla kaldırılan bir arkadaşlık aynı iki kişinin yeniden istek göndermesini engellemez — <see cref="UserConfiguration"/> ile <see cref="SubscriberConfiguration"/> aynı önlemi almaz. İndeks yönlüdür: yalnızca (isteyen, istenen) çiftini kapsar, ters yön veri tabanı için ayrı bir satırdır; çift kayıt engelleme işi servis katmanına aittir.</summary>
public class UserFriendConfiguration : BaseEntityConfiguration<UserFriend>
{
    public override void Configure(EntityTypeBuilder<UserFriend> builder)
    {
        base.Configure(builder);
        builder.ToTable("UserFriends");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Status>()
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.RequesterId, e.AddresseeId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(e => new { e.AddresseeId, e.StatusId });
    }
}
