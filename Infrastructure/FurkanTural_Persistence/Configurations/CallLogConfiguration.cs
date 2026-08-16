using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class CallLogConfiguration : BaseEntityConfiguration<CallLog>
{
    public override void Configure(EntityTypeBuilder<CallLog> builder)
    {
        base.Configure(builder);
        builder.ToTable("CallLogs");

        builder.Property(e => e.CallType).HasMaxLength(20);
        builder.Property(e => e.Status).HasMaxLength(20);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CallerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CalleeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.CallerId, e.StartedAt });
        builder.HasIndex(e => new { e.CalleeId, e.StartedAt });
    }
}