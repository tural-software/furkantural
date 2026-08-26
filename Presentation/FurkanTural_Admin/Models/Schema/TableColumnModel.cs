namespace FurkanTural_Admin.Models.Schema;

/// <summary>API'nin <c>TableColumnDto</c>'sunun lokal kopyası. Açıklama alanı API'de yoktur; sunum tarafında <see cref="FurkanTural_Admin.Helpers.SchemaDescriptions"/> ile doldurulur.</summary>
public sealed class TableColumnModel
{
    public string Name { get; set; } = string.Empty;
    public string ColumnType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public string? DefaultValue { get; set; }
}
