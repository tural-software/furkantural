using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Domain.Entities;

/// <summary>Blog kategorisi; <see cref="Blog"/>'a <see cref="BlogCategory"/> üzerinden çoğa-çok bağlanır. Color, kategori etiketinin hex rengidir.</summary>
public class Category : BaseEntity
{
    public string? Name { get; set; }
    public string? Color { get; set; }

    /// <summary>Kategorinin kalıcı adres parçası. Daha önce blog tarafında addan hesaplanıyordu; sütuna taşınmasının nedeni o çözümün iki sınırıydı — ad değişince eski adres 404 veriyor, iki ad aynı slug'a düşünce biri erişilemez kalıyordu. Sütun ikisini de kapatır: ad değişse de adres durur, çakışma yazma anında engellenir.</summary>
    public string? Slug { get; set; }
}
