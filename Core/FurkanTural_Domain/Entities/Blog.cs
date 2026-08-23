using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Blog yazısı. Görselleri <see cref="BlogImage"/> taşır, kategorileri <see cref="BlogCategory"/> ara tablosu üzerinden <see cref="Category"/>'ye bağlanır.</summary>
public class Blog : BaseEntity
{
    public string? Title { get; set; }
    public string? Content { get; set; }
}
