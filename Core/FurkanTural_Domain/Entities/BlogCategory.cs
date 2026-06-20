using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Blog ↔ Kategori çoğa-çok ara tablosu (FK-only; navigation property yok, proje deseniyle uyumlu).
/// </summary>
public class BlogCategory : BaseEntity
{
    public int BlogId { get; set; }
    public int CategoryId { get; set; }
}
