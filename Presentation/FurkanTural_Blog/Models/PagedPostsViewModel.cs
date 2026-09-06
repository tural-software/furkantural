namespace FurkanTural_Blog.Models;

/// <summary>Aynı listeyi gösteren üç sayfa var; hangisi olduğunu bu belirler. Liste gövdesi ortaktır, değişen yalnızca başlık ve bağlantıların hangi rotaya kurulacağıdır — sayfalama bağlantısı bulunduğu sayfada kalmalı, ana sayfaya düşmemelidir.</summary>
public enum BlogListKind
{
    Home,
    Category,
    Search
}

/// <summary>Liste sayfası için sayfalanmış blog yazıları + sayfalama + filtre durumu. Toplam sayfa istemcide (TotalCount/PageSize) hesaplanır; UI bununla beslenir.</summary>
public class PagedPostsViewModel
{
    public IReadOnlyList<BlogPostViewModel> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }

    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>Seçili kategori (null = tümü).</summary>
    public int? CategoryId { get; init; }

    /// <summary>Başlık araması (null/boş = arama yok).</summary>
    public string? Search { get; init; }

    /// <summary>Filtre çubuğundaki kategori seçenekleri.</summary>
    public IReadOnlyList<CategoryViewModel> AvailableCategories { get; init; } = [];

    public bool LoadFailed { get; init; }

    /// <summary>Herhangi bir filtre aktif mi (boş-sonuç mesajını ayarlamak için).</summary>
    public bool HasActiveFilter => CategoryId.HasValue || !string.IsNullOrWhiteSpace(Search);

    /// <summary>Listeyi gösteren sayfa. Ortak liste gövdesi bağlantılarını buna göre kurar.</summary>
    public BlogListKind Kind { get; set; } = BlogListKind.Home;

    /// <summary>Kategori sayfasında gösterilen kategori; başlık ve rengi buradan gelir. Diğer sayfalarda boştur.</summary>
    public CategoryViewModel? ActiveCategory { get; set; }
}
