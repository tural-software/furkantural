namespace FurkanTural_Admin.Models.Schema;

/// <summary>API'nin <c>TableSchemaDto</c>'sunun lokal kopyası.</summary>
public sealed class TableSchemaModel
{
    public string Entity { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<TableColumnModel> Columns { get; set; } = [];
}
