using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class SubscriberConfiguration : BaseEntityConfiguration<Subscriber>
{
    public override void Configure(EntityTypeBuilder<Subscriber> builder)
    {
        base.Configure(builder);
        builder.ToTable("Subscribers");
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.HasIndex(e => e.Email).IsUnique();
    }
}