using FurkanTural_Domain.Entities;
using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Application.DTOs.Category;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Tag;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class BlogService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IBlogService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<BlogDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<BlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        var dto = entity.ToDto();
        (dto.Categories, dto.Tags) = await TaxonomyAsync(id, cancellationToken);
        return Result<BlogDto>.Ok(dto);
    }

    /// <summary>Yazıyı kalıcı adres parçasıyla getirir. Eşleşme büyük-küçük harf ayrımı gözetmez; veri tabanı harmanlaması zaten öyle çalışır ve adres satırına büyük harfle yazan okur da aynı sayfaya varmalıdır.<para>Bulunamayan slug 404 döner, kimlik adresine düşmez: var olmayan bir adresin başka bir yazıyı açması, yanlış bağlantıyı sessizce doğru göstermek olurdu.</para></summary>
    public async Task<Result<BlogDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Result<BlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        var trimmed = slug.Trim();
        var entity = await _unitOfWork.Blogs.GetAsync(b => b.Slug == trimmed, cancellationToken);
        if (entity is null)
            return Result<BlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        var dto = entity.ToDto();
        (dto.Categories, dto.Tags) = await TaxonomyAsync(entity.Id, cancellationToken);
        return Result<BlogDto>.Ok(dto);
    }

    public async Task<Result<IEnumerable<BlogDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Blogs.GetAllAsync(cancellationToken);
        var dtos = entities.Select(e => e.ToDto()).ToList();
        await AttachTaxonomyAsync(dtos, cancellationToken);
        return Result<IEnumerable<BlogDto>>.Ok(dtos);
    }

    public async Task<Result<IEnumerable<AdminBlogDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Blogs.GetAllForAdminAsync(cancellationToken);
        var dtos = entities.Select(e => e.ToAdminDto()).ToList();
        await AttachTaxonomyAsync(dtos, cancellationToken);
        return Result<IEnumerable<AdminBlogDto>>.Ok(dtos);
    }

    public async Task<Result<AdminBlogDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        var dto = entity.ToAdminDto();
        (dto.Categories, dto.Tags) = await TaxonomyAsync(id, cancellationToken);
        return Result<AdminBlogDto>.Ok(dto);
    }

    public async Task<Result<AdminBlogDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminBlogDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Blogs.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Blog aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminBlogDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminBlogDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminBlogDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Blogs.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Blog geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminBlogDto>.Ok(entity.ToAdminDto());
    }

    public Task<PagedResult<BlogDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => GetPublishedPagedAsync(pageNumber, pageSize, categoryId: null, tagId: null, search: null, cancellationToken);

    public async Task<PagedResult<BlogDto>> GetPublishedPagedAsync(int pageNumber, int pageSize, int? categoryId, int? tagId, string? search, CancellationToken cancellationToken = default)
    {
        var (entities, total) = await _unitOfWork.Blogs.GetPublishedPageAsync(pageNumber, pageSize, categoryId, tagId, search, cancellationToken);
        var dtos = entities.Select(e => e.ToDto()).ToList();
        await AttachTaxonomyAsync(dtos, cancellationToken);
        return PagedResult<BlogDto>.Ok(dtos, total, pageNumber, pageSize);
    }

    public async Task<Result<IEnumerable<BlogSitemapDto>>> GetSitemapAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _unitOfWork.Blogs.GetSitemapDataAsync(cancellationToken);
        var dtos = rows.Select(r => new BlogSitemapDto
        {
            Id = r.Id,
            Title = r.Title,
            Slug = r.Slug,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
        return Result<IEnumerable<BlogSitemapDto>>.Ok(dtos);
    }

    public async Task<Result<BlogDto>> CreateAsync(CreateBlogDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<BlogDto>.Fail("Başlık boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Content))
            return Result<BlogDto>.Fail("İçerik boş olamaz.");

        var entity = dto.ToEntity();
        entity.Slug = await BuildSlugAsync(dto.Title, null, cancellationToken);
        await _unitOfWork.Blogs.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (dto.CategoryIds is { Count: > 0 } || dto.TagIds is { Count: > 0 })
        {
            if (dto.CategoryIds is { Count: > 0 })
                await _unitOfWork.Blogs.SetCategoriesAsync(entity.Id, dto.CategoryIds, dto.CreatedBy, cancellationToken);
            if (dto.TagIds is { Count: > 0 })
                await _unitOfWork.Blogs.SetTagsAsync(entity.Id, dto.TagIds, dto.CreatedBy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _activityLogger.LogAsync($"Blog oluşturuldu. Id: {entity.Id}", cancellationToken);

        var result = entity.ToDto();
        (result.Categories, result.Tags) = await TaxonomyAsync(entity.Id, cancellationToken);
        return Result<BlogDto>.Ok(result);
    }

    public async Task<Result<BlogDto>> UpdateAsync(UpdateBlogDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<BlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<BlogDto>.Fail("Başlık boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Content))
            return Result<BlogDto>.Fail("İçerik boş olamaz.");

        entity.UpdateEntity(dto);
        if (!string.IsNullOrWhiteSpace(dto.Slug))
            entity.Slug = await BuildSlugAsync(dto.Slug, entity.Id, cancellationToken);

        await _unitOfWork.Blogs.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (dto.CategoryIds is not null || dto.TagIds is not null)
        {
            if (dto.CategoryIds is not null)
                await _unitOfWork.Blogs.SetCategoriesAsync(entity.Id, dto.CategoryIds, dto.UpdatedBy, cancellationToken);
            if (dto.TagIds is not null)
                await _unitOfWork.Blogs.SetTagsAsync(entity.Id, dto.TagIds, dto.UpdatedBy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _activityLogger.LogAsync($"Blog güncellendi. Id: {entity.Id}", cancellationToken);

        var result = entity.ToDto();
        (result.Categories, result.Tags) = await TaxonomyAsync(entity.Id, cancellationToken);
        return Result<BlogDto>.Ok(result);
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Blog bulunamadı.", statusCode: 404);

        await _unitOfWork.Blogs.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Blog silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Blogs.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    /// <summary>Tek yazının kategorileri ve etiketleri. İki ayrı okuma yapar; tek sorguda birleştirmek iki farklı tabloyu aynı satıra çapraz çarpım olarak getirir ve her kategoriyi etiket sayısı kadar tekrarlardı.</summary>
    private async Task<(List<CategoryDto> Categories, List<TagDto> Tags)> TaxonomyAsync(int blogId, CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Blogs.GetCategoriesByBlogAsync(blogId, cancellationToken);
        var tags = await _unitOfWork.Blogs.GetTagsByBlogAsync(blogId, cancellationToken);
        return (categories.Select(c => c.ToDto()).ToList(), tags.Select(t => t.ToDto()).ToList());
    }

    /// <summary>Adresi başlıktan üretir ve çakışırsa sayı ekler. Aday listesi silinmiş satırları da kapsar: silinen bir yazının adresini başkasına vermek, o yazı geri yüklendiğinde tekil dizinde çakışırdı.<para>Yalnızca aynı önekle başlayan satırlar okunur ve okunan tek şey slug'dır; gövde bu sorguda hiç yer almaz. Önek eşleşmesi normalde sıfır ya da bir satır döndürür.</para></summary>
    private async Task<string> BuildSlugAsync(string? source, int? excludeId, CancellationToken cancellationToken)
    {
        var basis = Slugifier.ToSlug(source, SlugLimits.Blog, "yazi");

        var rows = await _unitOfWork.Blogs.SelectForAdminPagedAsync(
            1, SlugLimits.CollisionScan,
            b => new { b.Id, b.Slug },
            b => b.Slug != null && b.Slug.StartsWith(basis),
            cancellationToken: cancellationToken);

        var taken = rows
            .Where(x => excludeId is null || x.Id != excludeId)
            .Select(x => x.Slug!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Slugifier.MakeUnique(basis, SlugLimits.Blog, taken.Contains);
    }

    /// <summary>Listedeki bütün yazıların kategorileri ve etiketleri iki okumada toplanır — yazı başına değil. Sayfa boyutu ne olursa olsun maliyet sabit iki sorgudur.</summary>
    private async Task<(Dictionary<int, List<CategoryDto>> Categories, Dictionary<int, List<TagDto>> Tags)> TaxonomyMapAsync(
        IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Blogs.GetCategoriesForBlogsAsync(blogIds, cancellationToken);
        var tags = await _unitOfWork.Blogs.GetTagsForBlogsAsync(blogIds, cancellationToken);

        return (
            categories.ToDictionary(kv => kv.Key, kv => kv.Value.Select(c => c.ToDto()).ToList()),
            tags.ToDictionary(kv => kv.Key, kv => kv.Value.Select(t => t.ToDto()).ToList()));
    }

    private async Task AttachTaxonomyAsync(IReadOnlyList<BlogDto> dtos, CancellationToken cancellationToken)
    {
        if (dtos.Count == 0) return;
        var (categories, tags) = await TaxonomyMapAsync(dtos.Select(d => d.Id).ToList(), cancellationToken);
        foreach (var dto in dtos)
        {
            if (categories.TryGetValue(dto.Id, out var cats)) dto.Categories = cats;
            if (tags.TryGetValue(dto.Id, out var tagList)) dto.Tags = tagList;
        }
    }

    private async Task AttachTaxonomyAsync(IReadOnlyList<AdminBlogDto> dtos, CancellationToken cancellationToken)
    {
        if (dtos.Count == 0) return;
        var (categories, tags) = await TaxonomyMapAsync(dtos.Select(d => d.Id).ToList(), cancellationToken);
        foreach (var dto in dtos)
        {
            if (categories.TryGetValue(dto.Id, out var cats)) dto.Categories = cats;
            if (tags.TryGetValue(dto.Id, out var tagList)) dto.Tags = tagList;
        }
    }

    private static Expression<Func<Blog, bool>>? AdminPredicate(AdminListQuery query, int? blogId)
    {
        var predicate = AdminFilters.Common<Blog>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Title != null && x.Title.Contains(term));
        if (blogId is { } id)
            predicate = predicate.AndAlso(x => x.Id == id);
        return predicate;
    }

    public Task<PagedResult<AdminBlogDto>> GetAllForAdminPagedAsync(AdminListQuery query, int? blogId, CancellationToken cancellationToken = default)
        => GetAllForAdminPagedAsync(query, blogId, true, cancellationToken);

    private static readonly Expression<Func<Blog, Blog>> ListShape = x => new Blog
    {
        Id = x.Id,
        Title = x.Title,
        IsActive = x.IsActive,
        IsDeleted = x.IsDeleted,
        CreatedAt = x.CreatedAt,
        CreatedBy = x.CreatedBy,
        UpdatedAt = x.UpdatedAt,
        UpdatedBy = x.UpdatedBy,
        DeletedAt = x.DeletedAt,
        DeletedBy = x.DeletedBy
    };

    public async Task<PagedResult<AdminBlogDto>> GetAllForAdminPagedAsync(AdminListQuery query, int? blogId, bool includeContent, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query, blogId);
        var entities = includeContent
            ? await _unitOfWork.Blogs.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken)
            : await _unitOfWork.Blogs.SelectForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, ListShape, predicate, false, cancellationToken);
        var total = await _unitOfWork.Blogs.CountForAdminAsync(predicate, cancellationToken);

        var dtos = entities.Select(e => e.ToAdminDto()).ToList();
        await AttachTaxonomyAsync(dtos, cancellationToken);
        return PagedResult<AdminBlogDto>.Ok(dtos, total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, int? blogId, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.Blogs.GetAdminStatusCountsAsync(AdminPredicate(query, blogId), cancellationToken));

    public async Task<Result<IReadOnlyList<AdminOptionDto>>> GetAdminOptionsAsync(string? search, int? take, CancellationToken cancellationToken = default)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        Expression<Func<Blog, bool>>? predicate = term is null ? null : x => x.Title != null && x.Title.Contains(term);
        var options = await _unitOfWork.Blogs.GetAdminOptionsAsync(predicate, x => x.Title, x => new AdminOptionDto(x.Id, x.Title ?? ""), take, cancellationToken);
        return Result<IReadOnlyList<AdminOptionDto>>.Ok(options.Select(o => o.Label.Length > 0 ? o : o with { Label = $"Blog #{o.Id}" }).ToList());
    }

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.Blogs, action, ids, userId, "blog", _activityLogger, cancellationToken);
}