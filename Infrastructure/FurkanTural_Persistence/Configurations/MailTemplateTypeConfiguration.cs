using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Tohum satırları sabit Id taşır çünkü <see cref="MailTemplateConfiguration"/> tarafındaki taşıma bu Id'lere yaslanır; okuma tarafı yine de Id değil Code kullanır. Tohum SaveChangesAsync'ten geçmediği için CreatedAt elle verilir (bkz. <see cref="StatusConfiguration"/>, aynı gerekçe).<para>Code üzerindeki tekil indeks yumuşak silmeye göre süzülmez: silinmiş bir tür kodunun yeniden kullanılması, o kodu bekleyen gönderim yolunun sessizce başka bir satıra bağlanması demek olurdu.</para></summary>
public class MailTemplateTypeConfiguration : BaseEntityConfiguration<MailTemplateType>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public const int ContactOwnerId = 1;
    public const int ContactUserId = 2;
    public const int AccountActivationId = 3;

    public override void Configure(EntityTypeBuilder<MailTemplateType> builder)
    {
        base.Configure(builder);
        builder.ToTable("MailTemplateTypes");

        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(150);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasData(
            Seed(ContactOwnerId, MailTemplateDefinitions.ContactOwner, "İletişim — Site Sahibine",
                "İletişim formu doldurulduğunda site sahibine düşen bildirim.", 1),
            Seed(ContactUserId, MailTemplateDefinitions.ContactUser, "İletişim — Gönderene",
                "İletişim formunu dolduran kişiye giden alındı yanıtı.", 2),
            Seed(AccountActivationId, MailTemplateDefinitions.AccountActivation, "Hesap Aktivasyonu",
                "Pasife alınmış bir hesabı yeniden açan doğrulama bağlantısı.", 3));
    }

    private static MailTemplateType Seed(int id, string code, string name, string description, int sortOrder) => new()
    {
        Id = id,
        Code = code,
        Name = name,
        Description = description,
        SortOrder = sortOrder,
        CreatedAt = SeedDate,
        IsActive = true,
        IsDeleted = false
    };
}
