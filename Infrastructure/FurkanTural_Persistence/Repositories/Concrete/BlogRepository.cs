using Dapper;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Text;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>
/// Kategori bağı kuran okumalar EF üzerinden, sayfalama Dapper üzerinden koşar. Dapper tarafında
/// global sorgu süzgeci geçerli olmadığı için canlı satır koşulu <see cref="LiveRows"/> ile elle
/// eklenir; EF tarafında aynı koşul kendiliğinden uygulanır.
///
/// GetSitemapDataAsync yalnızca Id ile tarihleri projekte eder, dolayısıyla blog gövdesi veri
/// tabanından hiç çıkmaz.
/// </summary>
public class BlogRepository(FurkanTuralDbContext context) : Repository<Blog>(context), IBlogRepository
{
    public async Task<(IReadOnlyList<Blog> Items, int Total)> GetPublishedPageAsync(
        int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken cancellationToken = default)
    {
        var baseWhere = $"WHERE {LiveRows.FilterFor("b")}";

        var filterSb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(search))
            filterSb.Append(" AND b.Title LIKE @Search");
        if (categoryId.HasValue)
            filterSb.Append(" AND EXISTS (SELECT 1 FROM [BlogCategories] bc WHERE bc.BlogId = b.Id AND bc.CategoryId = @CategoryId)");

        var filter = filterSb.ToString();

        var sql = $"SELECT COUNT(*) FROM [Blogs] b {baseWhere}{filter};" +
                  $" SELECT b.* FROM [Blogs] b {baseWhere}{filter}" +
                  " ORDER BY b.Id DESC OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY";

        var parameters = new
        {
            Search     = !string.IsNullOrWhiteSpace(search) ? $"%{search.Trim()}%" : (string?)null,
            CategoryId = categoryId,
            Offset     = (pageNumber - 1) * pageSize,
            Size       = pageSize
        };

        var conn = (DbConnection)_context.Database.GetDbConnection();
        if (conn.State == ConnectionState.Closed)
            await conn.OpenAsync(cancellationToken);

        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<Blog>()).AsList();

        return (items, total);
    }

    public async Task<IReadOnlyList<(int Id, DateTime CreatedAt, DateTime? UpdatedAt)>> GetSitemapDataAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Set<Blog>().AsNoTracking()
            .OrderByDescending(b => b.Id)
            .Select(b => new { b.Id, b.CreatedAt, b.UpdatedAt })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Id, r.CreatedAt, r.UpdatedAt)).ToList();
    }

    public async Task<Dictionary<int, List<Category>>> GetCategoriesForBlogsAsync(
        IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken = default)
    {
        if (blogIds.Count == 0)
            return [];

        var links = _context.Set<BlogCategory>();
        var categories = _context.Set<Category>();

        var rows = await (from bc in links.AsNoTracking()
                          join c in categories.AsNoTracking() on bc.CategoryId equals c.Id
                          where blogIds.Contains(bc.BlogId)
                          select new { bc.BlogId, Category = c })
                         .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.BlogId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Category).OrderBy(c => c.Name).ToList());
    }

    public async Task<List<Category>> GetCategoriesByBlogAsync(int blogId, CancellationToken cancellationToken = default)
    {
        var links = _context.Set<BlogCategory>();
        var categories = _context.Set<Category>();

        return await (from bc in links.AsNoTracking()
                      join c in categories.AsNoTracking() on bc.CategoryId equals c.Id
                      where bc.BlogId == blogId
                      orderby c.Name
                      select c).ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetCategoryIdsByBlogAsync(int blogId, CancellationToken cancellationToken = default)
    {
        var links = _context.Set<BlogCategory>();
        return await links.AsNoTracking()
            .Where(bc => bc.BlogId == blogId)
            .Select(bc => bc.CategoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task SetCategoriesAsync(int blogId, IReadOnlyCollection<int> categoryIds, int? userId, CancellationToken cancellationToken = default)
    {
        var links = _context.Set<BlogCategory>();
        var existing = await links.Where(bc => bc.BlogId == blogId).ToListAsync(cancellationToken);
        var wanted = categoryIds.Distinct().ToHashSet();

        var toRemove = existing.Where(bc => !wanted.Contains(bc.CategoryId)).ToList();
        if (toRemove.Count > 0)
            links.RemoveRange(toRemove);

        var existingIds = existing.Select(bc => bc.CategoryId).ToHashSet();
        foreach (var cid in wanted)
            if (!existingIds.Contains(cid))
                await links.AddAsync(new BlogCategory { BlogId = blogId, CategoryId = cid, CreatedBy = userId }, cancellationToken);
    }
}