using FurkanTural_Domain.Abstract;

namespace FurkanTural_Domain.Entities.Common;

/// <summary>Tüm entity'lerin ortak tabanı; ilişkiler navigation property ile değil yalnızca FK alanıyla kurulur. IsActive ve IsDeleted birlikte global sorgu filtresini oluşturur (BaseEntityConfiguration): sorgulara yalnızca <c>!IsDeleted &amp;&amp; IsActive</c> satırlar girer, yani pasife almak da silmek kadar görünmez kılar. CreatedAt/UpdatedAt/DeletedAt elle set edilmez, AuditSaveChangesInterceptor damgalar; CreatedBy/UpdatedBy/DeletedBy servisin sorumluluğunda.</summary>
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}
