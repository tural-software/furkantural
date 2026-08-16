using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>
/// Kullanıcı adı ve e-posta tekil indekslerinin ikisi de yumuşak silmeye göre süzülmez; e-postadaki
/// süzgeç yalnızca boş değerleri indeks dışında tutar. Sonuç şudur: silinmiş bir kullanıcının adı ve
/// e-postası satır tabloda durmaya devam ettiği için kalıcı olarak rezerve kalır.
///
/// Bunu kayıt akışıyla birlikte okumak gerekir: oradaki varlık kontrolü EF üzerinden geçtiği için
/// global süzgeç yüzünden silinmiş satırı göremez, adres boştaymış gibi davranır ve ekleme tekil
/// indekse takılır. Aynı tuzaktan kaçınan örnek için <see cref="UserFriendConfiguration"/>.
/// </summary>
public class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.ToTable("Users");
        builder.Property(e => e.Username).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Password).HasMaxLength(500).IsRequired();
        builder.HasIndex(e => e.Username).IsUnique();

        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.DisplayName).HasMaxLength(150);
        builder.Property(e => e.AvatarUrl).HasMaxLength(500);
        builder.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}