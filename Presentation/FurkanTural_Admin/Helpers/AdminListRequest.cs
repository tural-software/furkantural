using System.Globalization;
using System.Text;

namespace FurkanTural_Admin.Helpers;

/// <summary>Liste sayfasının süzgeçlerini API'nin sorgu dizesine çeviren tek yer. Panelin süzgeç sözcükleri ("active"/"passive", "deleted"/"notDeleted") burada bool'a iner; tarih metinleri bugüne kadar olduğu gibi DateTime.TryParse ile okunur ki kullanıcının gördüğü davranış değişmesin. Modüle özel süzgeçler With ile eklenir ve aynı dizeye girer; aynı anahtar birden çok değerle tekrarlanabilir (statuses=a&amp;statuses=b). Sayfalı liste ile sayaçlar aynı süzgeci taşıdığından iki uç da aynı nesneden beslenir.</summary>
public sealed record AdminListRequest
{
    public static readonly AdminListRequest Unfiltered = new();

    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsDeleted { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public IReadOnlyList<KeyValuePair<string, string>> Extras { get; init; } = [];

    public static AdminListRequest From(
        string? search, string? activeFilter, string? deletedFilter, string? dateFrom, string? dateTo,
        int pageNumber, int pageSize) =>
        new()
        {
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            IsActive = activeFilter switch { "active" => true, "passive" => false, _ => null },
            IsDeleted = deletedFilter switch { "deleted" => true, "notDeleted" => false, _ => null },
            DateFrom = DateTime.TryParse(dateFrom, out var from) ? from : null,
            DateTo = DateTime.TryParse(dateTo, out var to) ? to : null,
            PageNumber = pageNumber > 0 ? pageNumber : 1,
            PageSize = pageSize is > 0 and <= 100 ? pageSize : 10
        };

    public AdminListRequest With(string key, object? value)
    {
        var text = Render(value);
        if (text is null) return this;
        return this with { Extras = [.. Extras, new KeyValuePair<string, string>(key, text)] };
    }

    public AdminListRequest WithAll(string key, IEnumerable<string?> values)
    {
        var added = values.Select(Render).Where(v => v is not null).Select(v => new KeyValuePair<string, string>(key, v!)).ToList();
        return added.Count == 0 ? this : this with { Extras = [.. Extras, .. added] };
    }

    public string? Extra(string key)
        => Extras.FirstOrDefault(p => p.Key == key).Value;

    private static string? Render(object? value)
    {
        var text = value switch
        {
            null => null,
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public string ToQueryString(string path, bool paged)
    {
        var qs = new StringBuilder(path).Append('?');
        var first = true;

        void Add(string key, string value)
        {
            if (!first) qs.Append('&');
            qs.Append(key).Append('=').Append(Uri.EscapeDataString(value));
            first = false;
        }

        if (paged)
        {
            Add("pageNumber", PageNumber.ToString(CultureInfo.InvariantCulture));
            Add("pageSize", PageSize.ToString(CultureInfo.InvariantCulture));
        }
        if (Search is not null) Add("search", Search);
        if (IsActive is { } active) Add("isActive", active ? "true" : "false");
        if (IsDeleted is { } deleted) Add("isDeleted", deleted ? "true" : "false");
        if (DateFrom is { } from) Add("dateFrom", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (DateTo is { } to) Add("dateTo", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        foreach (var (key, value) in Extras) Add(key, value);

        return first ? path : qs.ToString();
    }
}
