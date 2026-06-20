namespace FurkanTural_Blog.Models;

/// <summary>
/// Liste sayfası için sayfalanmış blog yazıları + sayfalama + filtre durumu.
/// Toplam sayfa istemcide (TotalCount/PageSize) hesaplanır; UI bununla beslenir.
/// </summary>
public class PagedPostsViewModel
{
    public IReadOnlyList<BlogPostViewModel> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }

    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    // ── Filtre durumu ────────────────────────────────────────
    /// <summary>Seçili kategori (null = tümü).</summary>
    public int? CategoryId { get; init; }

    /// <summary>Başlık araması (null/boş = arama yok).</summary>
    public string? Search { get; init; }

    /// <summary>Filtre çubuğundaki kategori seçenekleri.</summary>
    public IReadOnlyList<CategoryViewModel> AvailableCategories { get; init; } = [];

    /// <summary>Herhangi bir filtre aktif mi (boş-sonuç mesajını ayarlamak için).</summary>
    public bool HasActiveFilter => CategoryId.HasValue || !string.IsNullOrWhiteSpace(Search);
}
