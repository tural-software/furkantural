using Dapper;
using FurkanTural_Domain.Entities.Common;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Persistence.Contexts;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>
/// Sözleşmenin melez uygulaması. Ayrımı belirleyen kural şudur: sabit bir SQL cümlesine çevrilebilen
/// okumalar Dapper'a, yüklem alanlar EF Core'a gider. Yüklem EF'de kalmak zorundadır çünkü ifade
/// ağacını SQL'e çeviren şey EF'in kendisidir; Dapper böyle bir girdiyi tüketemez.
///
/// İki yol da aynı DbConnection üzerinden koşar, Dapper ayrı bağlantı açmaz. Bunun görünür sonucu
/// şudur: henüz SaveChangesAsync görmemiş bir değişiklik Dapper okumasına yansımaz, çünkü o değişiklik
/// hâlâ değişiklik izleyicisinde durur ve veri tabanına yazılmamıştır.
///
/// Tablo adı sabit yazılmaz, EF modelinden çalışma anında okunur — konfigürasyondaki ToTable değişimi
/// ham SQL'lere kendiliğinden yansır. Yumuşak silme süzgeci ise <see cref="LiveRows"/> üzerinden gelir.
/// Bağlam alanı, türetilmiş repo'lar başka entity'lerin Set'lerine ulaşabilsin diye korumalıdır.
/// </summary>
public class Repository<T>(FurkanTuralDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly FurkanTuralDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();
    private string? _tableName;
    private string TableName => _tableName ??= _context.Model.FindEntityType(typeof(T))!.GetTableName()!;

    private async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var conn = (DbConnection)_context.Database.GetDbConnection();
        if (conn.State == ConnectionState.Closed)
            await conn.OpenAsync(cancellationToken);
        return conn;
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<T>(new CommandDefinition(
            $"SELECT * FROM [{TableName}] WHERE Id = @Id AND {LiveRows.Filter}",
            new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<T?> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<T>(new CommandDefinition(
            $"SELECT * FROM [{TableName}] WHERE Id = @Id",
            new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<T>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<T>(new CommandDefinition(
            $"SELECT * FROM [{TableName}]",
            cancellationToken: cancellationToken));
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<T>(new CommandDefinition(
            $"SELECT * FROM [{TableName}] WHERE {LiveRows.Filter}",
            cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task<IEnumerable<T>> GetAllPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, bool descending = false, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
        {
            var query = _dbSet.AsNoTracking().Where(predicate);
            query = descending ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id);
            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        var order = descending ? "DESC" : "ASC";
        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<T>(new CommandDefinition(
            $"SELECT * FROM [{TableName}] WHERE {LiveRows.Filter} " +
            $"ORDER BY Id {order} OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY",
            new { Offset = (pageNumber - 1) * pageSize, Size = pageSize },
            cancellationToken: cancellationToken));
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
            return await _dbSet.CountAsync(predicate, cancellationToken);

        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(*) FROM [{TableName}] WHERE {LiveRows.Filter}",
            cancellationToken: cancellationToken));
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(predicate, cancellationToken);

    public async Task<EntitySummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetOpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleAsync<SummaryRow>(new CommandDefinition(
            $"""
            SELECT
                COUNT(*)  AS [Count],
                MAX(CASE
                    WHEN UpdatedAt IS NOT NULL THEN UpdatedAt
                    WHEN DeletedAt IS NOT NULL THEN DeletedAt
                    ELSE CreatedAt
                END)      AS [LastModifiedDate]
            FROM [{TableName}]
            """,
            cancellationToken: cancellationToken));
        return new EntitySummaryDto(row.Count, row.LastModifiedDate);
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

    private sealed class SummaryRow
    {
        public int Count { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}