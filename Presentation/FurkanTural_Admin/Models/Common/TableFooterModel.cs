namespace FurkanTural_Admin.Models.Common;

/// <summary>Liste alt bandının ihtiyacı: sayfa durumu ve o anki süzgeçler. Süzgeçler sıralı tutulur çünkü hem sayfa bağlantısının sorgu dizesini hem de sayfa boyutu formunun gizli alanlarını aynı liste üretir; ikisi ayrışırsa sayfa değişince süzgeç düşer.</summary>
public sealed record TableFooterModel(
    int PageNumber,
    int PageSize,
    int TotalPages,
    int TotalFiltered,
    IReadOnlyList<KeyValuePair<string, object?>> Filters);
