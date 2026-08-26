using FurkanTural_Application.DTOs.Schema;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>EF Core modelinden tablo şeması okur. Veri değil metadata döndürür; hiçbir satıra dokunmaz. Uygulaması veri katmanındadır çünkü model yalnızca orada bilinir.<para>Entity adının geçerliliğini denetlemez — o denetim çağıranın sorumluluğundadır ve <see cref="FurkanTural_Domain.Constants.SchemaEntityDefinitions"/> üzerinden yapılır. Modelde karşılığı bulunmayan ad için null döner.</para></summary>
public interface ISchemaMetadataReader
{
    TableSchemaDto? Read(string entity);
}
