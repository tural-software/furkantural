namespace FurkanTural_Blog.Models;

/// <summary>Blog kategorisi — kart chip'leri ve filtre için.</summary>
public class CategoryViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }

    /// <summary>Chip rengi; tanımsızsa site accent'ine düşer.</summary>
    public string DisplayColor => string.IsNullOrWhiteSpace(Color) ? "var(--accent)" : Color!;
}