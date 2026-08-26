namespace FurkanTural_Admin.Models.Schema;

/// <summary>Tablo şeması sayfasının tek modeli. Özet sayıları burada hesaplanır, taşınmaz; şemanın kendisi API'den geldiği için sayı ile kolon listesi ayrışamaz.</summary>
public sealed class TableSchemaViewModel
{
    public string ModuleTitle { get; set; } = string.Empty;
    public string ModuleUrl { get; set; } = string.Empty;
    public TableSchemaModel? Schema { get; set; }
    public string? ErrorMessage { get; set; }

    public IReadOnlyList<TableColumnModel> Columns => Schema?.Columns ?? [];

    public int TotalCount => Columns.Count;
    public int PrimaryKeyCount => Columns.Count(c => c.IsPrimaryKey);
    public int IdentityCount => Columns.Count(c => c.IsIdentity);
    public int RequiredCount => Columns.Count(c => !c.IsNullable);
    public int NullableCount => Columns.Count(c => c.IsNullable);
    public int DefaultCount => Columns.Count(c => c.DefaultValue is not null);
}
