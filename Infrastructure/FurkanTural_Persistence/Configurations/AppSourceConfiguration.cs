using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>Tohum satırları sabit Id taşır çünkü <see cref="MailTemplateConfiguration"/> tarafındaki şablon tohumu bu Id'lere yaslanır; okuma tarafı yine de Id değil Code kullanır. Tohum SaveChangesAsync'ten geçmediği için CreatedAt elle verilir (bkz. <see cref="StatusConfiguration"/>, aynı gerekçe).<para>Code üzerindeki tekil indeks yumuşak silmeye göre süzülmez: silinmiş bir kaynak kodunun yeniden kullanılması, o adı taşıyan şablonların sessizce başka bir projeye bağlanması demek olurdu.</para></summary>
public class AppSourceConfiguration : BaseEntityConfiguration<AppSource>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public const int PortfolioId = 1;
    public const int BlogId = 2;
    public const int ChatId = 3;
    public const int AdminId = 4;

    public override void Configure(EntityTypeBuilder<AppSource> builder)
    {
        base.Configure(builder);
        builder.ToTable("AppSources");

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(150);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasData(
            Seed(PortfolioId, AppSourceDefinitions.Portfolio, "Portfolyo",
                "Genel portfolyo sitesi; iletişim formu buradadır.", 1),
            Seed(BlogId, AppSourceDefinitions.Blog, "Blog",
                "Genel blog sitesi.", 2),
            Seed(ChatId, AppSourceDefinitions.Chat, "Chatural",
                "Sohbet uygulaması; kullanıcı hesapları buradadır.", 3),
            Seed(AdminId, AppSourceDefinitions.Admin, "Yönetim Paneli",
                "Yönetim paneli; app-token'ı yoktur, adı hiçbir claim'de geçmez.", 4));
    }

    private static AppSource Seed(int id, string code, string name, string description, int sortOrder) => new()
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
