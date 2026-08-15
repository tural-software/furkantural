using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// <see cref="Blog"/> ile <see cref="Category"/> arasındaki çoğa-çok ara tablosu.
/// </summary>
public class BlogCategory : BaseEntity
{
    public int BlogId { get; set; }
    public int CategoryId { get; set; }
}
