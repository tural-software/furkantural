namespace FurkanTural_Blog.Models;

/// <summary>Blog kategorisi — kart chip'leri, filtre rayı ve kategori sayfası için.</summary>
public class CategoryViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }

    /// <summary>Chip rengi; tanımsızsa site accent'ine düşer.</summary>
    public string DisplayColor => string.IsNullOrWhiteSpace(Color) ? "var(--accent)" : Color!;

    /// <summary>Kategori sayfasının adres parçası; API'den gelir. Daha önce addan hesaplanıyordu ve o çözümün iki sınırı vardı — ad değişince eski adres 404 veriyor, iki ad aynı slug'a düşünce biri erişilemez kalıyordu. Artık sütunda tutuluyor, ikisi de kapandı.<para>Boş varsayılan bilerektir: alan hiç gelmezse değer null değil boş dizedir ve o kategori kendi sayfasına bağlanmaz. Kimliği olmayan bir adres üretmektense bağlantı hiç verilmez.</para></summary>
    public string Slug { get; set; } = string.Empty;
}
