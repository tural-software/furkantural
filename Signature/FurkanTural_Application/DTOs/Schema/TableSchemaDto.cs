namespace FurkanTural_Application.DTOs.Schema;

/// <summary>Bir entity'nin tablo şeması. Özet sayıları (toplam alan, birincil anahtar, zorunlu, boş geçilebilir) burada taşınmaz; <see cref="Columns"/> üzerinden hesaplanır. Sayıyı ağdan geçirmek, bugün elle sayılmış sabitlerin aynı sapma riskini tekrarlardı.</summary>
public sealed class TableSchemaDto
{
    public required string Entity { get; set; }
    public required string TableName { get; set; }
    public List<TableColumnDto> Columns { get; set; } = [];
}
