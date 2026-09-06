using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary><see cref="Blog"/> ile <see cref="Tag"/> arasındaki çoğa-çok ara tablosu.</summary>
public class BlogTag : BaseEntity
{
    public int BlogId { get; set; }
    public int TagId { get; set; }
}
