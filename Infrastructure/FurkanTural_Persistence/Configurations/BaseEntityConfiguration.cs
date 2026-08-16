using FurkanTural_Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FurkanTural_Persistence.Configurations;

/// <summary>
/// Her entity'nin ortak şeması. Buradaki global sorgu süzgeci en geniş etkili karardır: EF üzerinden
/// koşan her sorgu — birleştirmeler dahil — silinmiş ve pasif satırları kendiliğinden eler, bu yüzden
/// ham SQL yazan Dapper okumaları aynı koşulu elle taşımak zorundadır.
///
/// IsActive ve IsDeleted için verilen varsayılanlar şemaya yazılır ama EF yolundan pratikte devreye
/// girmezler; ekleme anında aynı alanları denetim damgalayıcısı zaten sabitler.
/// </summary>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(e => !e.IsDeleted && e.IsActive);
    }
}