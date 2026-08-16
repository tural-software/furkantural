using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class ChatMessageConfiguration : BaseEntityConfiguration<ChatMessage>
{
    public override void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        base.Configure(builder);
        builder.ToTable("ChatMessages");

        builder.Property(e => e.Content).HasColumnType("nvarchar(max)");
        builder.Property(e => e.MessageType).HasMaxLength(20);
        builder.Property(e => e.AttachmentUrl).HasMaxLength(300);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SenderId, e.ReceiverId, e.CreatedAt });
    }
}