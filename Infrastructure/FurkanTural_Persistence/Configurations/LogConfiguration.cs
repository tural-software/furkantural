using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class LogConfiguration : BaseEntityConfiguration<Log>
{
    public override void Configure(EntityTypeBuilder<Log> builder)
    {
        base.Configure(builder);
        builder.ToTable("Logs");
        builder.Property(e => e.Project).HasMaxLength(200);
        builder.Property(e => e.Level).HasMaxLength(50);
        builder.Property(e => e.Message).HasMaxLength(1000);
        builder.Property(e => e.Detail).HasColumnType("nvarchar(max)");
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.Path).HasMaxLength(500);
    }
}
