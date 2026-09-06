using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Tag;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Etiket yönetimi; gövde olarak <see cref="CategoryService"/> ile aynı kalıptadır. İki yerde ayrılır.<para>Birincisi ad tekilliği: veri tabanındaki tekil indeks kuralı taşır, buradaki denetim yalnızca kullanıcıya okunur bir mesaj vermek içindir. İki katman da gerekli — yalnızca burada denetlemek eşzamanlı iki isteği kaçırır, yalnızca veri tabanına bırakmak kullanıcıya çakışma yanıtı gösterir.</para><para>İkincisi yazı sayısı: yönetim listesi ve bulut, etiket başına ayrı sorgu açmadan tek okumada sayıyı toplar. Sayım blog satırından geçtiği için pasife alınmış yazı hiçbir etiketin sayısına girmez.</para></summary>
public class TagService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : ITagService
{
    /// <summary>Bulutta gösterilecek en fazla etiket. Sınır sunum kararıdır ama burada durur: sınırsız bir bulut, etiket sayısı arttıkça sayfanın kendisini yutardı.</summary>
    public const int MaxPopular = 40;

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<TagDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Tags.GetByIdAsync(id, cancellationToken);
        return entity is null
            ? Result<TagDto>.Fail("Etiket bulunamadı.", statusCode: 404)
            : Result<TagDto>.Ok(entity.ToDto());
    }

    public async Task<Result<TagDto>> GetBySlugAsync(string? slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Result<TagDto>.Fail("Etiket bulunamadı.", statusCode: 404);

        var trimmed = slug.Trim();
        var entity = await _unitOfWork.Tags.GetAsync(t => t.Slug == trimmed, cancellationToken);
        return entity is null
            ? Result<TagDto>.Fail("Etiket bulunamadı.", statusCode: 404)
            : Result<TagDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<TagDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Tags.GetAllAsync(cancellationToken);
        return Result<IEnumerable<TagDto>>.Ok(entities.OrderBy(t => t.Name).Select(e => e.ToDto()));
    }

    public async Task<PagedResult<TagDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Tags.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Tags.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<TagDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<IEnumerable<AdminTagDto>>> GetPopularAsync(int take, CancellationToken cancellationToken = default)
    {
        var limit = take <= 0 ? MaxPopular : Math.Min(take, MaxPopular);
        var entities = (await _unitOfWork.Tags.GetAllAsync(cancellationToken)).ToList();
        var counts = await CountsAsync(entities, cancellationToken);

        var popular = entities
            .Select(t => t.ToAdminDto(counts.GetValueOrDefault(t.Id)))
            .Where(t => t.PostCount > 0)
            .OrderByDescending(t => t.PostCount)
            .ThenBy(t => t.Name)
            .Take(limit);

        return Result<IEnumerable<AdminTagDto>>.Ok(popular);
    }

    public async Task<Result<TagDto>> CreateAsync(CreateTagDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<TagDto>.Fail("Etiket adı boş olamaz.");

        var name = dto.Name.Trim();
        if (name.Length > 80)
            return Result<TagDto>.Fail("Etiket adı en fazla 80 karakter olabilir.");

        if (await NameTakenAsync(name, null, cancellationToken))
            return Result<TagDto>.Fail("Bu adda bir etiket zaten var.", statusCode: 409);

        var entity = dto.ToEntity();
        entity.Slug = await BuildSlugAsync(name, null, cancellationToken);

        await _unitOfWork.Tags.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Etiket oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<TagDto>.Ok(entity.ToDto());
    }

    public async Task<Result<TagDto>> UpdateAsync(UpdateTagDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Tags.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<TagDto>.Fail("Etiket bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<TagDto>.Fail("Etiket adı boş olamaz.");

        var name = dto.Name.Trim();
        if (name.Length > 80)
            return Result<TagDto>.Fail("Etiket adı en fazla 80 karakter olabilir.");

        if (await NameTakenAsync(name, entity.Id, cancellationToken))
            return Result<TagDto>.Fail("Bu adda bir etiket zaten var.", statusCode: 409);

        entity.UpdateEntity(dto);
        if (!string.IsNullOrWhiteSpace(dto.Slug))
            entity.Slug = await BuildSlugAsync(dto.Slug, entity.Id, cancellationToken);

        await _unitOfWork.Tags.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Etiket güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<TagDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Tags.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Etiket bulunamadı.", statusCode: 404);

        await _unitOfWork.Tags.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Etiket silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminTagDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = (await _unitOfWork.Tags.GetAllForAdminAsync(cancellationToken)).ToList();
        var counts = await CountsAsync(entities, cancellationToken);
        return Result<IEnumerable<AdminTagDto>>.Ok(entities.Select(e => e.ToAdminDto(counts.GetValueOrDefault(e.Id))));
    }

    public async Task<Result<AdminTagDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Tags.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminTagDto>.Fail("Etiket bulunamadı.", statusCode: 404);

        var counts = await CountsAsync([entity], cancellationToken);
        return Result<AdminTagDto>.Ok(entity.ToAdminDto(counts.GetValueOrDefault(entity.Id)));
    }

    public async Task<Result<AdminTagDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Tags.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminTagDto>.Fail("Etiket bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminTagDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Tags.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Etiket aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminTagDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminTagDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Tags.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminTagDto>.Fail("Etiket bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminTagDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Tags.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Etiket geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminTagDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
        => Result<EntitySummaryDto>.Ok(await _unitOfWork.Tags.GetAdminSummaryAsync(cancellationToken));

    public async Task<PagedResult<AdminTagDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query);
        var entities = (await _unitOfWork.Tags.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken)).ToList();
        var total = await _unitOfWork.Tags.CountForAdminAsync(predicate, cancellationToken);
        var counts = await CountsAsync(entities, cancellationToken);

        return PagedResult<AdminTagDto>.Ok(
            entities.Select(e => e.ToAdminDto(counts.GetValueOrDefault(e.Id))), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.Tags.GetAdminStatusCountsAsync(AdminPredicate(query), cancellationToken));

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.Tags, action, ids, userId, "etiket", _activityLogger, cancellationToken);

    private Task<Dictionary<int, int>> CountsAsync(IReadOnlyCollection<Tag> tags, CancellationToken cancellationToken)
        => tags.Count == 0
            ? Task.FromResult(new Dictionary<int, int>())
            : _unitOfWork.Blogs.GetPostCountsForTagsAsync([.. tags.Select(t => t.Id)], cancellationToken);

    /// <summary>Ad çakışması silinmiş satırları da kapsar: tekil indeks yumuşak silmeye göre süzülmez, dolayısıyla silinmiş bir etiketin adını yeniden kullanmak kaydetme anında çakışırdı.</summary>
    private async Task<bool> NameTakenAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Tags.GetAllForAdminAsync(t => t.Name == name, cancellationToken);
        return rows.Any(t => excludeId is null || t.Id != excludeId);
    }

    private static Expression<Func<Tag, bool>>? AdminPredicate(AdminListQuery query)
    {
        var predicate = AdminFilters.Common<Tag>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Name != null && x.Name.Contains(term));
        return predicate;
    }

    /// <summary>Adresi etiket adından üretir ve çakışırsa sayı ekler. Aday listesi silinmiş satırları da kapsar; gerekçesi <see cref="NameTakenAsync"/> ile aynıdır.</summary>
    private async Task<string> BuildSlugAsync(string? source, int? excludeId, CancellationToken cancellationToken)
    {
        var basis = Slugifier.ToSlug(source, SlugLimits.Tag, "etiket");

        var rows = await _unitOfWork.Tags.SelectForAdminPagedAsync(
            1, SlugLimits.CollisionScan,
            t => new Tag { Id = t.Id, Slug = t.Slug },
            t => t.Slug != null && t.Slug.StartsWith(basis),
            cancellationToken: cancellationToken);

        var taken = rows
            .Where(x => excludeId is null || x.Id != excludeId)
            .Select(x => x.Slug!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Slugifier.MakeUnique(basis, SlugLimits.Tag, taken.Contains);
    }
}
