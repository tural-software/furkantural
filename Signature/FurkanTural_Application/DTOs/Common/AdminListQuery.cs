namespace FurkanTural_Application.DTOs.Common;

/// <summary>Yönetici listelerinin ortak süzgeç ve sayfa sözleşmesi. Bütün alanlar isteğe bağlıdır ve verilenler birlikte uygulanır; Search hangi sütunlarda arandığını bilmez, o karar modülün servisindedir. DateTo gün olarak okunur ve dışlayıcı üst sınıra çevrilir: 04.09 verildiğinde 05.09 00:00'dan küçük satırlar girer, yani günün tamamı kapsanır. Sayfa değerleri sınırlanır; 100'den büyük sayfa boyu varsayılana düşer.</summary>
public sealed record AdminListQuery
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsDeleted { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;

    public int SafePageNumber => PageNumber > 0 ? PageNumber : 1;
    public int SafePageSize => PageSize is > 0 and <= MaxPageSize ? PageSize : DefaultPageSize;
    public DateTime? DateToExclusive => DateTo?.Date.AddDays(1);
    public string? SearchTerm => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

    public static AdminListQuery From(
        string? search, bool? isActive, bool? isDeleted, DateTime? dateFrom, DateTime? dateTo,
        int pageNumber = 1, int pageSize = DefaultPageSize) =>
        new()
        {
            Search = search,
            IsActive = isActive,
            IsDeleted = isDeleted,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
}
