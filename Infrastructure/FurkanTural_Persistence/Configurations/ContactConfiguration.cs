using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class ContactConfiguration : BaseEntityConfiguration<Contact>
{
    public override void Configure(EntityTypeBuilder<Contact> builder)
    {
        base.Configure(builder);
        builder.ToTable("Contacts");
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.Message).HasMaxLength(2000);
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
    }
}