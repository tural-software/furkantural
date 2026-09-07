using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Yanıt bildirimi kuyruğu. <see cref="CommentNotification.CommentId"/> tekildir ve kural veri tabanında durur: aynı yanıt için ikinci bir satır açılırsa okur aynı bildirimi iki kez alır, bunu geri almanın yolu yoktur. Reddedilip yeniden onaylanan bir yanıt bu yüzden ikinci posta üretmez.<para>İndeks yumuşak silmeye göre süzülmez — silinmiş bir bildirim satırı da "bu yanıt zaten duyuruldu" bilgisini taşır.</para><para>Yorum bağı Restrict'tir. Gönderim kaydı, yanıtın kendisi ortadan kalksa bile durmalıdır: bir adrese ne gönderdiğimizin cevabı yalnızca burada yazılıdır.</para></summary>
public class CommentNotificationConfiguration : BaseEntityConfiguration<CommentNotification>
{
    public override void Configure(EntityTypeBuilder<CommentNotification> builder)
    {
        base.Configure(builder);
        builder.ToTable("CommentNotifications");

        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(CommentNotificationStatuses.MaxLength).IsRequired();
        builder.Property(e => e.Error).HasMaxLength(500);
        builder.Property(e => e.TokenHash).HasMaxLength(64);

        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(e => e.CommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.CommentId).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.TokenHash);
    }
}
