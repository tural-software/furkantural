using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class StatusConfiguration : BaseEntityConfiguration<Status>
{
    // HasData, SaveChangesAsync'i atladığı için CreatedAt sabit verilir.
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public override void Configure(EntityTypeBuilder<Status> builder)
    {
        base.Configure(builder);
        builder.ToTable("Statuses");

        builder.Property(e => e.Group).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(150);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Color).HasMaxLength(32);

        builder.HasIndex(e => new { e.Group, e.Code }).IsUnique();

        builder.HasData(
            FriendshipStatus(1, StatusDefinitions.FriendshipCodes.Pending, "Beklemede", "#f59e0b", 1),
            FriendshipStatus(2, StatusDefinitions.FriendshipCodes.Accepted, "Kabul Edildi", "#10b981", 2),
            FriendshipStatus(3, StatusDefinitions.FriendshipCodes.Rejected, "Reddedildi", "#ef4444", 3),
            FriendshipStatus(4, StatusDefinitions.FriendshipCodes.Blocked, "Engellendi", "#6b7280", 4));
    }

    private static Status FriendshipStatus(int id, string code, string name, string color, int sortOrder) => new()
    {
        Id = id,
        Group = StatusDefinitions.Groups.Friendship,
        Code = code,
        Name = name,
        Color = color,
        SortOrder = sortOrder,
        CreatedAt = SeedDate,
        IsActive = true,
        IsDeleted = false
    };
}
