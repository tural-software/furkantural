using FurkanTural_Admin.Models.Schema;

namespace FurkanTural_Admin.Services;

public interface ISchemaApiClient
{
    Task<TableSchemaModel?> GetAsync(string entity, string token, CancellationToken ct = default);
}
