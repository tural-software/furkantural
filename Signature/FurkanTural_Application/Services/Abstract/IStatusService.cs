using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Status;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IStatusService : IService<StatusDto, CreateStatusDto, UpdateStatusDto>
{
    Task<Result<IEnumerable<StatusDto>>> GetByGroupAsync(string group, CancellationToken cancellationToken = default);

    /// <summary>Belirli grup + kod için statü Id'sini çözer (kodda hardcoded id kullanmamak için).</summary>
    Task<int?> GetIdByCodeAsync(string group, string code, CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<AdminStatusDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminStatusDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
