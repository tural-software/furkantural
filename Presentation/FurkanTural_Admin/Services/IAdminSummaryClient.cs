using FurkanTural_Admin.Models.Common;

namespace FurkanTural_Admin.Services;

public interface IAdminSummaryClient
{
    Task<EntitySummaryModel?> GetAsync(string entityPath, string token, CancellationToken cancellationToken = default);
}
