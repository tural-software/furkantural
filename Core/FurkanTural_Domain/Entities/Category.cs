using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>
/// Blog kategorisi. Blog ile çoğa-çok ilişkilidir (<see cref="BlogCategory"/> ara tablosu).
/// <see cref="Color"/> kart üzerindeki kategori chip'inin rengidir (hex; boşsa UI varsayılan accent kullanır).
/// </summary>
public class Category : BaseEntity
{
    public string? Name { get; set; }
    public string? Color { get; set; }
}
