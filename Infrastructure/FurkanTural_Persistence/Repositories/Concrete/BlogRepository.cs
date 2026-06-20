using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>
/// Genel <see cref="Repository{T}"/> davranışına ek olarak blog-kategori (çoğa-çok) sorguları
/// ve kategori/başlık filtreli sayfalama sağlar. Global query filter (!IsDeleted &amp;&amp; IsActive)
/// tüm sorgulara otomatik uygulanır.
/// </summary>
public class BlogRepository(FurkanTuralDbContext context) : Repository<Blog>(context), IBlogRepository
{
    public async Task<(IReadOnlyList<Blog> Items, int Total)> GetPublishedPageAsync(
        int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b => b.Title != null && EF.Functions.Like(b.Title, $"%{term}%"));
        }

        if (categoryId is int cid)
        {
            var links = context.Set<BlogCategory>();
            query = query.Where(b => links.Any(bc => bc.BlogId == b.Id && bc.CategoryId == cid));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Dictionary<int, List<Category>>> GetCategoriesForBlogsAsync(
        IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken = default)
    {
        if (blogIds.Count == 0)
            return [];

        var links = context.Set<BlogCategory>();
        var categories = context.Set<Category>();

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
        var links = context.Set<BlogCategory>();
        var categories = context.Set<Category>();

        return await (from bc in links.AsNoTracking()
                      join c in categories.AsNoTracking() on bc.CategoryId equals c.Id
                      where bc.BlogId == blogId
                      orderby c.Name
                      select c).ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetCategoryIdsByBlogAsync(int blogId, CancellationToken cancellationToken = default)
    {
        var links = context.Set<BlogCategory>();
        return await links.AsNoTracking()
            .Where(bc => bc.BlogId == blogId)
            .Select(bc => bc.CategoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task SetCategoriesAsync(int blogId, IReadOnlyCollection<int> categoryIds, int? userId, CancellationToken cancellationToken = default)
    {
        var links = context.Set<BlogCategory>();
        var existing = await links.Where(bc => bc.BlogId == blogId).ToListAsync(cancellationToken);
        var wanted = categoryIds.Distinct().ToHashSet();

        // İstenmeyenleri kaldır (ara tablo satırları sert silinir — soft-delete biriktirmez).
        var toRemove = existing.Where(bc => !wanted.Contains(bc.CategoryId)).ToList();
        if (toRemove.Count > 0)
            links.RemoveRange(toRemove);

        // Eksik olanları ekle.
        var existingIds = existing.Select(bc => bc.CategoryId).ToHashSet();
        foreach (var cid in wanted)
            if (!existingIds.Contains(cid))
                await links.AddAsync(new BlogCategory { BlogId = blogId, CategoryId = cid, CreatedBy = userId }, cancellationToken);
    }
}
