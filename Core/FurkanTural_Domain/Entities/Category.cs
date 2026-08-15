using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Blog kategorisi; <see cref="Blog"/>'a <see cref="BlogCategory"/> üzerinden çoğa-çok bağlanır.
/// Color, kategori etiketinin hex rengidir.
/// </summary>
public class Category : BaseEntity
{
    public string? Name { get; set; }
    public string? Color { get; set; }
}
