using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Sistem ve istemci logları. Yalnızca yazılır ve okunur; güncelleme, silme veya aktiflik değiştirme ucu yoktur, dolayısıyla log satırları uygulama üzerinden değiştirilemez. GetAllForAdminPagedAsync'in tüm filtreleri opsiyoneldir ve verilenler birlikte uygulanır.</summary>
public interface ILogService
{
    Task<Result<LogDto>> CreateAsync(CreateLogDto dto, CancellationToken cancellationToken = default);
    Task<Result<LogDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<LogDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<LogDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<LogDto>> GetAllForAdminPagedAsync(string? level, string? source, string? message, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
