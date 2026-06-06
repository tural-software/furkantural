using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class CallPolicyConfiguration : BaseEntityConfiguration<CallPolicy>
{
    // HasData, SaveChangesAsync'i atladığı için CreatedAt sabit verilir.
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public override void Configure(EntityTypeBuilder<CallPolicy> builder)
    {
        base.Configure(builder);
        builder.ToTable("CallPolicies");

        // Tek küresel satır (Id=1).
        builder.HasData(new CallPolicy
        {
            Id = 1,
            BitrateLimitEnabled = CallDefinitions.PolicyDefaults.BitrateLimitEnabled,
            MaxVideoBitrateKbps = CallDefinitions.PolicyDefaults.MaxVideoBitrateKbps,
            MaxWidth = CallDefinitions.PolicyDefaults.MaxWidth,
            MaxHeight = CallDefinitions.PolicyDefaults.MaxHeight,
            MaxFps = CallDefinitions.PolicyDefaults.MaxFps,
            CreatedAt = SeedDate,
            IsActive = true,
            IsDeleted = false
        });
    }
}
