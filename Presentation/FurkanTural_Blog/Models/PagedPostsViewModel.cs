namespace FurkanTural_Blog.Models;

/// <summary>
/// Liste sayfası için sayfalanmış blog yazıları + sayfalama durumu.
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
}
