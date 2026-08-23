using Dapper;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>Sözleşmenin kendi başına duran uygulaması: <see cref="Repository{T}"/> türetmez, bağlantı açma ve sayfalama mantığını kendi içinde tekrarlar. Tablo adı da oradaki gibi EF modelinden okunmaz, sabit yazılıdır — Logs tablosu konfigürasyondan yeniden adlandırılırsa buradaki SQL'ler derleme hatası vermeden kırılır.</summary>
public class LogRepository(FurkanTuralDbContext context) : ILogRepository
{
    private readonly FurkanTuralDbContext _context = context;
    private readonly DbSet<Log> _dbSet = context.Set<Log>();
    private const string Table = "Logs";

    private async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var conn = (DbConnection)_context.Database.GetDbConnection();
        if (conn.State == ConnectionState.Closed)
            await conn.OpenAsync(cancellationToken);
        return conn;
    }

    public async Task AddAsync(Log log, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(log, cancellationToken);

    public async Task<Log?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<Log>(new CommandDefinition(
            $"SELECT * FROM [{Table}] WHERE Id = @Id AND {LiveRows.Filter}",
            new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Log>> GetAllAsync(Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<Log>(new CommandDefinition(
            $"SELECT * FROM [{Table}] WHERE {LiveRows.Filter} ORDER BY Date DESC",
            cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Log>> GetAllPagedAsync(int pageNumber, int pageSize, Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
            return await _dbSet.AsNoTracking()
                .Where(predicate)
                .OrderByDescending(l => l.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<Log>(new CommandDefinition(
            $"SELECT * FROM [{Table}] WHERE {LiveRows.Filter} " +
            $"ORDER BY Date DESC OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY",
            new { Offset = (pageNumber - 1) * pageSize, Size = pageSize },
            cancellationToken: cancellationToken));
    }

    public async Task<int> CountAsync(Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
            return await _dbSet.CountAsync(predicate, cancellationToken);

        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(*) FROM [{Table}] WHERE {LiveRows.Filter}",
            cancellationToken: cancellationToken));
    }

    public async Task<EntitySummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetOpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleAsync<SummaryRow>(new CommandDefinition(
            $"SELECT COUNT(*) AS [Count], MAX(CreatedAt) AS [LastModifiedDate] " +
            $"FROM [{Table}] WHERE {LiveRows.Filter}",
            cancellationToken: cancellationToken));
        return new EntitySummaryDto(row.Count, row.LastModifiedDate);
    }

    public async Task<IEnumerable<Log>> GetAllForAdminPagedAsync(
        string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (where, parameters) = BuildAdminWhere(level, project, message, dateFrom, dateTo);
        parameters.Add("Offset", (pageNumber - 1) * pageSize);
        parameters.Add("Size", pageSize);

        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<Log>(new CommandDefinition(
            $"SELECT * FROM [{Table}] {where} ORDER BY Date DESC OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY",
            parameters, cancellationToken: cancellationToken));
    }

    public async Task<int> CountForAdminAsync(
        string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (where, parameters) = BuildAdminWhere(level, project, message, dateFrom, dateTo);

        var conn = await GetOpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(*) FROM [{Table}] {where}",
            parameters, cancellationToken: cancellationToken));
    }

    private static (string Where, DynamicParameters Parameters) BuildAdminWhere(
        string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo)
    {
        var sql = new StringBuilder($"WHERE {LiveRows.Filter}");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(level))
        {
            sql.Append(" AND Level = @Level");
            parameters.Add("Level", level);
        }

        if (!string.IsNullOrWhiteSpace(project))
        {
            sql.Append(" AND Project LIKE @Project");
            parameters.Add("Project", $"%{project}%");
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            sql.Append(" AND Message LIKE @Message");
            parameters.Add("Message", $"%{message}%");
        }

        if (dateFrom.HasValue)
        {
            sql.Append(" AND Date >= @DateFrom");
            parameters.Add("DateFrom", dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            sql.Append(" AND Date < @DateTo");
            parameters.Add("DateTo", dateTo.Value.AddDays(1));
        }

        return (sql.ToString(), parameters);
    }

    private sealed class SummaryRow
    {
        public int Count { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
