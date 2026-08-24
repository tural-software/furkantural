using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Tür başına tek etkin şablon kuralı buradaki süzgeçli tekil indekstir; servis katmanında değil veri tabanında durur, çünkü aynı türe iki etkin şablon yazıldığında gönderim hangisini seçeceğini bilemez ve seçim sıralamaya göre sessizce değişirdi.<para>Süzgeç yalnızca etkin ve silinmemiş satırları kapsar, dolayısıyla taslak ve arşiv sayısı sınırsızdır. İkinci bir şablonu etkinleştirme denemesi kısıta takılır ve <see cref="FurkanTural_Application.Exceptions.DuplicateEntityException"/> üzerinden temiz bir çakışma yanıtına dönüşür.</para><para>Tür bağı Restrict'tir: şablonu duran bir tür kalıcı olarak silinemez. Yumuşak silme bundan etkilenmez, o yüzden kural yalnızca elle yapılacak bir silmede kendini gösterir.</para></summary>
public class MailTemplateConfiguration : BaseEntityConfiguration<MailTemplate>
{
    public override void Configure(EntityTypeBuilder<MailTemplate> builder)
    {
        base.Configure(builder);
        builder.ToTable("MailTemplates");

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Subject).HasMaxLength(300);
        builder.Property(e => e.HtmlContent).HasColumnType("nvarchar(max)");
        builder.Property(e => e.FileName).HasMaxLength(200);

        builder.HasOne<MailTemplateType>()
            .WithMany()
            .HasForeignKey(e => e.MailTemplateTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.MailTemplateTypeId)
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
    }
}
