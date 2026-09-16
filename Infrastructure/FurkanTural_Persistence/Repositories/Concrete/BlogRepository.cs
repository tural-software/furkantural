using Dapper;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Text;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>Kategori bağı kuran okumalar EF üzerinden, sayfalama Dapper üzerinden koşar. Dapper tarafında global sorgu süzgeci geçerli olmadığı için canlı satır koşulu <see cref="LiveRows"/> ile elle eklenir; EF tarafında aynı koşul kendiliğinden uygulanır.<para>GetSitemapDataAsync yalnızca Id ile tarihleri projekte eder, dolayısıyla blog gövdesi veri tabanından hiç çıkmaz.</para></summary>
public class BlogRepository(FurkanTuralDbContext context) : Repository<Blog>(context), IBlogRepository
{
    public async Task<(IReadOnlyList<Blog> Items, int Total)> GetPublishedPageAsync(
        int pageNumber, int pageSize, int? categoryId, int? tagId, string? search, CancellationToken cancellationToken = default)
    {
        var baseWhere = $"WHERE {LiveRows.FilterFor("b")}";

        var filterSb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(search))
            filterSb.Append(" AND b.Title LIKE @Search" + LikePattern.EscapeClause);
        if (categoryId.HasValue)
            filterSb.Append(" AND EXISTS (SELECT 1 FROM [BlogCategories] bc WHERE bc.BlogId = b.Id AND bc.CategoryId = @CategoryId)");
        if (tagId.HasValue)
            filterSb.Append(" AND EXISTS (SELECT 1 FROM [BlogTags] bt WHERE bt.BlogId = b.Id AND bt.TagId = @TagId)");

        var filter = filterSb.ToString();

        var sql = $"SELECT COUNT(*) FROM [Blogs] b {baseWhere}{filter};" +
                  $" SELECT b.* FROM [Blogs] b {baseWhere}{filter}" +
                  " ORDER BY b.Id DESC OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY";

        var parameters = new
        {
            Search     = !string.IsNullOrWhiteSpace(search) ? LikePattern.Contains(search.Trim()) : (string?)null,
            CategoryId = categoryId,
            TagId      = tagId,
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

    public async Task<IReadOnlyList<(int Id, string? Title, string? Slug, DateTime CreatedAt, DateTime? UpdatedAt)>> GetSitemapDataAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Set<Blog>().AsNoTracking()
            .OrderByDescending(b => b.Id)
            .Select(b => new { b.Id, b.Title, b.Slug, b.CreatedAt, b.UpdatedAt })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Id, r.Title, r.Slug, r.CreatedAt, r.UpdatedAt)).ToList();
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

    public async Task<Dictionary<int, List<Tag>>> GetTagsForBlogsAsync(
        IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken = default)
    {
        if (blogIds.Count == 0)
            return [];

        var links = _context.Set<BlogTag>();
        var tags = _context.Set<Tag>();

        var rows = await (from bt in links.AsNoTracking()
                          join t in tags.AsNoTracking() on bt.TagId equals t.Id
                          where blogIds.Contains(bt.BlogId)
                          select new { bt.BlogId, Tag = t })
                         .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.BlogId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Tag).OrderBy(t => t.Name).ToList());
    }

    public async Task<List<Tag>> GetTagsByBlogAsync(int blogId, CancellationToken cancellationToken = default)
    {
        var links = _context.Set<BlogTag>();
        var tags = _context.Set<Tag>();

        return await (from bt in links.AsNoTracking()
                      join t in tags.AsNoTracking() on bt.TagId equals t.Id
                      where bt.BlogId == blogId
                      orderby t.Name
                      select t).ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetTagIdsByBlogAsync(int blogId, CancellationToken cancellationToken = default)
    {
        var links = _context.Set<BlogTag>();
        return await links.AsNoTracking()
            .Where(bt => bt.BlogId == blogId)
            .Select(bt => bt.TagId)
            .ToListAsync(cancellationToken);
    }

    public async Task SetTagsAsync(int blogId, IReadOnlyCollection<int> tagIds, int? userId, CancellationToken cancellationToken = default)
    {
        var links = _context.Set<BlogTag>();
        var existing = await links.Where(bt => bt.BlogId == blogId).ToListAsync(cancellationToken);
        var wanted = tagIds.Distinct().ToHashSet();

        var toRemove = existing.Where(bt => !wanted.Contains(bt.TagId)).ToList();
        if (toRemove.Count > 0)
            links.RemoveRange(toRemove);

        var existingIds = existing.Select(bt => bt.TagId).ToHashSet();
        foreach (var tid in wanted)
            if (!existingIds.Contains(tid))
                await links.AddAsync(new BlogTag { BlogId = blogId, TagId = tid, CreatedBy = userId }, cancellationToken);
    }

    /// <summary>Sayım bağ tablosunun kendi süzgecinden değil blog satırından geçer: pasife alınmış ya da silinmiş bir yazı etiketin sayısına girmemeli, çünkü o etiketin sayfasında da görünmeyecek.</summary>
    public async Task<Dictionary<int, int>> GetPostCountsForTagsAsync(
        IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default)
    {
        if (tagIds.Count == 0)
            return [];

        var links = _context.Set<BlogTag>();
        var blogs = _context.Set<Blog>();

        var rows = await (from bt in links.AsNoTracking()
                          join b in blogs.AsNoTracking() on bt.BlogId equals b.Id
                          where tagIds.Contains(bt.TagId)
                          group bt by bt.TagId into g
                          select new { TagId = g.Key, Count = g.Count() })
                         .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.TagId, r => r.Count);
    }

    /// <summary>Sayaç satır yüklenmeden artırılır. Okuyup değiştirip kaydetmek iki gidiş dönüş ister ve aynı anda okuyan iki kişi aynı eski değeri okuyup aynı yeni değeri yazardı; tek deyimde artırmak bunu veri tabanına havale eder.<para>Koşul canlı satırla sınırlıdır, dolayısıyla yayından kalkmış bir yazının sayacı adresine elle istek gönderilerek şişirilemez. Sıfır satır güncellendiyse çağıran bunu <c>false</c> olarak görür.</para></summary>
    public async Task<bool> IncrementViewCountAsync(int blogId, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<Blog>()
            .Where(b => b.Id == blogId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.ViewCount, b => b.ViewCount + 1), cancellationToken);

        return affected > 0;
    }
}
