using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Status;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Uygulama genelinde kullanılan durum satırları. Gerçek anahtar Id değil Group ve Code ikilisidir; servisler statüyü <see cref="FurkanTural_Domain.Constants.StatusDefinitions"/> sabitleri üzerinden çözer, böylece Id'ler ortamlar arasında farklı olabilir. GetIdByCodeAsync <see cref="Wrappers.Result"/> zarfı kullanmaz: bulunamazsa null döner ve eksik statünün ne anlama geldiğine çağıran karar verir.</summary>
public interface IStatusService : IService<StatusDto, CreateStatusDto, UpdateStatusDto>, IBulkService
{
    Task<Result<IEnumerable<StatusDto>>> GetByGroupAsync(string group, CancellationToken cancellationToken = default);
    Task<int?> GetIdByCodeAsync(string group, string code, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminStatusDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminStatusDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminStatusDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? group, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? group, CancellationToken cancellationToken = default);
}
