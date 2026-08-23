using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Tablo çok kayıtlı bir liste değil tekil ayar kaydıdır; tohumlanan Id = 1 satırı tek küresel politikadır. Tohum SaveChangesAsync'ten geçmediği için CreatedAt elle verilir.</summary>
public class CallPolicyConfiguration : BaseEntityConfiguration<CallPolicy>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public override void Configure(EntityTypeBuilder<CallPolicy> builder)
    {
        base.Configure(builder);
        builder.ToTable("CallPolicies");

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
