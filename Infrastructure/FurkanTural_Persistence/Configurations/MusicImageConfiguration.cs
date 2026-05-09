using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class MusicImageConfiguration : BaseEntityConfiguration<MusicImage>
{
    public override void Configure(EntityTypeBuilder<MusicImage> builder)
    {
        base.Configure(builder);
        builder.ToTable("MusicImages");
        builder.Property(e => e.Url).HasMaxLength(1000);
        builder.Property(e => e.AltText).HasMaxLength(500);
        builder.HasOne<Music>()
            .WithMany()
            .HasForeignKey(e => e.MusicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}