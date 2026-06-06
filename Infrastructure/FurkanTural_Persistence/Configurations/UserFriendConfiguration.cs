using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

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

        // Aynı yönde tek aktif ilişki; soft-delete edilenler yeniden eklemeyi engellemesin.
        builder.HasIndex(e => new { e.RequesterId, e.AddresseeId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Bekleyen istek sorgusu için.
        builder.HasIndex(e => new { e.AddresseeId, e.StatusId });
    }
}
