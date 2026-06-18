using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class MusicConfiguration : BaseEntityConfiguration<Music>
{
    public override void Configure(EntityTypeBuilder<Music> builder)
    {
        base.Configure(builder);
        builder.ToTable("Musics");
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Artist).HasMaxLength(200);
        builder.Property(e => e.Productor).HasMaxLength(200);
        builder.Property(e => e.Album).HasMaxLength(200);
        builder.Property(e => e.Genre).HasMaxLength(200);
        builder.Property(e => e.Lyrics).HasColumnType("nvarchar(max)");
        builder.Property(e => e.YouTubeMusicUrl).HasMaxLength(500);
    }
}