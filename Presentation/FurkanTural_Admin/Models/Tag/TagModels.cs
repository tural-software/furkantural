namespace FurkanTural_Admin.Models.Tag;

public sealed class TagAdminDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }

    /// <summary>Etikete bağlı yayındaki yazı sayısı. Pasife alınmış ya da silinmiş yazı sayılmaz; etiketin sayfasında da görünmeyeceği için sayının onunla aynı şeyi söylemesi gerekir.</summary>
    public int PostCount { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}

public sealed class TagFormDto
{
    public string? Name { get; set; }

    /// <summary>Kalıcı adres parçası. Boş bırakılırsa mevcut adres korunur; dolu gönderilirse eski adres kırılır ve bu bilinçli bir karardır.</summary>
    public string? Slug { get; set; }
}

public sealed class TagIndexViewModel
{
    public IReadOnlyList<TagAdminDto> Rows { get; init; } = [];

    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int PassiveCount { get; init; }
    public int DeletedCount { get; init; }

    /// <summary>Hiçbir yazıya bağlı olmayan etiket sayısı. Ayrı bir sayaç olarak durur çünkü etiket listesi zamanla kullanılmayan satırlarla dolar ve bunu görmeden temizlemek mümkün değildir.</summary>
    public int UnusedCount { get; init; }

    public string? SearchName { get; init; }
    public string? ActiveFilter { get; init; }
    public string? DeletedFilter { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalFiltered { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalFiltered / PageSize) : 0;
}
