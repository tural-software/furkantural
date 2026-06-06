using FurkanTural_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

public class ContactTemplateConfiguration : BaseEntityConfiguration<ContactTemplate>
{
    public override void Configure(EntityTypeBuilder<ContactTemplate> builder)
    {
        base.Configure(builder);
        builder.ToTable("ContactTemplates");
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.TemplateType).HasMaxLength(20);
        builder.Property(e => e.FileName).HasMaxLength(200);
        builder.Property(e => e.HtmlContent).HasColumnType("nvarchar(max)");
    }
}
