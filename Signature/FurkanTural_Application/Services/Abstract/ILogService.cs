using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface ILogService
{
    Task<Result<LogDto>> CreateAsync(CreateLogDto dto, CancellationToken cancellationToken = default);
    Task<Result<LogDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<LogDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<LogDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}