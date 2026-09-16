namespace FurkanTural_Application.DTOs.Retention;

public sealed record DataRetentionCategoryDto(string Key, string Label, int Count);

/// <summary>Saklama süresi dolmuş verinin kategori bazında dökümü. Önizlemede silinecek, silmeden sonra silinmiş satır sayılarını taşır. Cutoff bu sınırdan eski olanların silindiği andır.</summary>
public sealed class DataRetentionReportDto
{
    public DateTime Cutoff { get; set; }

    public IReadOnlyList<DataRetentionCategoryDto> Categories { get; set; } = [];

    public int FilesDeleted { get; set; }

    public int FilesFailed { get; set; }

    public int Total => Categories.Sum(c => c.Count);
}
