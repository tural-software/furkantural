using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

public interface ILogRepository
{
    Task AddAsync(Log log, CancellationToken cancellationToken = default);
    Task<Log?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> GetAllAsync(Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> GetAllPagedAsync(int pageNumber, int pageSize, Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> GetAllForAdminPagedAsync(string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountForAdminAsync(string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
    Task<EntitySummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}