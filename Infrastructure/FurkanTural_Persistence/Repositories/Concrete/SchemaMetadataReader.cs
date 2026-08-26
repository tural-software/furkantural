using FurkanTural_Application.DTOs.Schema;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>Tablo şemasını EF Core'un <see cref="IModel"/> yapısından okur. Kaynak modelin kendisi olduğu için okunan değer ile veri tabanının hâli ayrışamaz — elle tutulan bir şema belgesinin kaçınılmaz olarak kaydığı yer tam da burasıdır.<para>Entity, CLR tipinin adıyla aranır; modelde bulunamazsa null döner. Kolon sırası modeldeki bildirim sırasıdır.</para></summary>
public sealed class SchemaMetadataReader(FurkanTuralDbContext context) : ISchemaMetadataReader
{
    private readonly FurkanTuralDbContext _context = context;

    public TableSchemaDto? Read(string entity)
    {
        var entityType = _context.Model
            .GetEntityTypes()
            .FirstOrDefault(t => string.Equals(t.ClrType.Name, entity, StringComparison.Ordinal));

        if (entityType is null)
            return null;

        var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);

        var columns = entityType.GetProperties()
            .Select(property => new TableColumnDto
            {
                Name = storeObject.HasValue
                    ? property.GetColumnName(storeObject.Value) ?? property.Name
                    : property.Name,
                ColumnType = property.GetColumnType(),
                MaxLength = property.GetMaxLength(),
                IsNullable = property.IsNullable,
                IsPrimaryKey = property.IsPrimaryKey(),
                IsIdentity = property.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn,
                DefaultValue = ReadDefault(property)
            })
            .ToList();

        return new TableSchemaDto
        {
            Entity = entityType.ClrType.Name,
            TableName = entityType.GetTableName() ?? entityType.ClrType.Name,
            Columns = columns
        };
    }

    /// <summary>Yalnızca gerçekten yapılandırılmış varsayılanı döndürür. <c>GetDefaultValue()</c> tek başına kullanılamaz: yapılandırılmamış değer tiplerinde CLR karşılığını (int için 0, DateTime için 0001-01-01) verir ve şema sayfasına var olmayan bir varsayılan yazılmasına yol açar.</summary>
    private static string? ReadDefault(IProperty property)
    {
        var sql = property.GetDefaultValueSql();
        if (!string.IsNullOrWhiteSpace(sql))
            return sql;

        return property.TryGetDefaultValue(out var value) ? value?.ToString() : null;
    }
}
