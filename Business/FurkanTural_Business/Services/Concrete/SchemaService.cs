using FurkanTural_Application.DTOs.Schema;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Domain.Constants;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Tablo şeması okumasının güvenlik sınırı burasıdır. İstekten gelen ad önce <see cref="SchemaEntityDefinitions"/> beyaz listesine vurulur; listede yoksa modele hiç bakılmaz ve 404 döner. Bu sıra bilinçlidir: sınır aşılırsa uç, EF modelindeki her tipi yoklamak için kullanılabilir hâle gelir.<para>Beyaz listede olup modelde bulunmayan ad da 404 döner — dışarıdan bakan için iki durum ayırt edilemez.</para></summary>
public class SchemaService(ISchemaMetadataReader reader) : ISchemaService
{
    private readonly ISchemaMetadataReader _reader = reader;

    public Result<TableSchemaDto> Get(string? entity)
    {
        if (!SchemaEntityDefinitions.IsAllowed(entity))
            return Result<TableSchemaDto>.Fail("Tablo bulunamadı.", $"Beyaz listede olmayan entity istendi: {entity}", statusCode: 404);

        var schema = _reader.Read(entity!);
        if (schema is null)
            return Result<TableSchemaDto>.Fail("Tablo bulunamadı.", $"Beyaz listede olan ama modelde bulunmayan entity: {entity}", statusCode: 404);

        return Result<TableSchemaDto>.Ok(schema);
    }
}
