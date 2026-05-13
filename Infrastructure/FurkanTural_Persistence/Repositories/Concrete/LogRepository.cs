using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

public class LogRepository(FurkanTuralDbContext context) : ILogRepository
{
    private readonly DbSet<Log> _dbSet = context.Set<Log>();

    public async Task AddAsync(Log log, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(log, cancellationToken);

    public async Task<Log?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<Log>> GetAllAsync(Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (predicate != null) query = query.Where(predicate);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Log>> GetAllPagedAsync(int pageNumber, int pageSize, Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (predicate != null) query = query.Where(predicate);
        return await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null
            ? await _dbSet.CountAsync(cancellationToken)
            : await _dbSet.CountAsync(predicate, cancellationToken);

    public async Task<EntitySummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        var count = await query.CountAsync(cancellationToken);
        if (count == 0)
            return new EntitySummaryDto(0, null);

        var lastCreated = await query.MaxAsync(e => (DateTime?)e.CreatedAt, cancellationToken);
        return new EntitySummaryDto(count, lastCreated);
    }

    private IQueryable<Log> ApplyAdminFilters(IQueryable<Log> query, string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(e => e.Level == level);

        if (!string.IsNullOrWhiteSpace(project))
            query = query.Where(e => e.Project != null && e.Project.Contains(project));

        if (!string.IsNullOrWhiteSpace(message))
            query = query.Where(e => e.Message != null && e.Message.Contains(message));

        if (dateFrom.HasValue)
            query = query.Where(e => e.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(e => e.Date < dateTo.Value.AddDays(1));

        return query;
    }

    public async Task<IEnumerable<Log>> GetAllForAdminPagedAsync(string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyAdminFilters(_dbSet.AsNoTracking().AsQueryable(), level, project, message, dateFrom, dateTo);
        return await query
            .OrderByDescending(e => e.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountForAdminAsync(string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default)
    {
        var query = ApplyAdminFilters(_dbSet.AsNoTracking().AsQueryable(), level, project, message, dateFrom, dateTo);
        return await query.CountAsync(cancellationToken);
    }
}