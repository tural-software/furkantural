using FurkanTural_Domain.Entities.Common;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

public class Repository<T>(FurkanTuralDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public async Task<T?> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IEnumerable<T>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
        => await _dbSet.IgnoreQueryFilters().AsNoTracking().ToListAsync(cancellationToken);

    public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task<IEnumerable<T>> GetAllPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (predicate != null) query = query.Where(predicate);
        return await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null
            ? await _dbSet.CountAsync(cancellationToken)
            : await _dbSet.CountAsync(predicate, cancellationToken);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(predicate, cancellationToken);

    public async Task<EntitySummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var query = _dbSet.IgnoreQueryFilters().AsNoTracking();

        var count = await query.CountAsync(cancellationToken);
        if (count == 0)
            return new EntitySummaryDto(0, null);

        var maxCreated = await query.MaxAsync(e => (DateTime?)e.CreatedAt, cancellationToken);
        var maxUpdated = await query.MaxAsync(e => e.UpdatedAt, cancellationToken);
        var maxDeleted = await query.MaxAsync(e => e.DeletedAt, cancellationToken);

        DateTime? last = null;
        foreach (var t in new[] { maxCreated, maxUpdated, maxDeleted })
            if (t.HasValue && (last is null || t.Value > last.Value))
                last = t.Value;

        return new EntitySummaryDto(count, last);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await _dbSet.AddRangeAsync(entities, cancellationToken);

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task RestoreAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.IsDeleted = false;
        entity.IsActive = true;
        entity.DeletedAt = null;
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }
}