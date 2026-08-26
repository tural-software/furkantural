namespace FurkanTural_Application.DTOs.Schema;

/// <summary>Bir tablo kolonunun şema bilgisi. Değerler EF Core modelinden okunur, elle yazılmaz; bu yüzden veri tabanıyla ayrışamazlar. Açıklama alanı burada taşınmaz — o insan metnidir ve sunum katmanında tutulur.</summary>
public sealed class TableColumnDto
{
    public required string Name { get; set; }
    public required string ColumnType { get; set; }
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public string? DefaultValue { get; set; }
}
