using FurkanTural_Blog.Helpers;

namespace FurkanTural_Blog.Models;

/// <summary>Blog kategorisi — kart chip'leri, filtre rayı ve kategori sayfası için.</summary>
public class CategoryViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }

    /// <summary>Chip rengi; tanımsızsa site accent'ine düşer.</summary>
    public string DisplayColor => string.IsNullOrWhiteSpace(Color) ? "var(--accent)" : Color!;

    /// <summary>Kategori sayfasının adres parçası; addan üretilir, şemada karşılığı yoktur (bkz. <see cref="Slugifier"/>). Ad boşsa boş döner ve o kategori kendi sayfasına bağlanmaz — kimliği olmayan bir adres üretmektense bağlantı hiç verilmez.</summary>
    public string Slug => Slugifier.ToSlug(Name);
}
