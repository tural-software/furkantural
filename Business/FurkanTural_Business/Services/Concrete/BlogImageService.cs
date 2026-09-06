using FurkanTural_Domain.Entities;
using System.Linq.Expressions;
using FurkanTural_Application.DTOs.BlogImage;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class BlogImageService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IBlogImageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<BlogImageDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.BlogImages.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<BlogImageDto>.Fail("Blog görseli bulunamadı.", statusCode: 404);

        return Result<BlogImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<BlogImageDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.BlogImages.GetAllAsync(cancellationToken);
        return Result<IEnumerable<BlogImageDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<BlogImageDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.BlogImages.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.BlogImages.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<BlogImageDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<IEnumerable<BlogImageDto>>> GetByBlogIdAsync(int blogId, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.BlogImages.GetAllAsync(x => x.BlogId == blogId, cancellationToken);
        return Result<IEnumerable<BlogImageDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<Result<IEnumerable<AdminBlogImageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.BlogImages.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminBlogImageDto>>.Ok(entities.Select(x => x.ToAdminDto()));
    }

    public async Task<Result<AdminBlogImageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.BlogImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogImageDto>.Fail("Blog görseli bulunamadı.", statusCode: 404);

        return Result<AdminBlogImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminBlogImageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.BlogImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogImageDto>.Fail("Blog görseli bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminBlogImageDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.BlogImages.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"BlogImage aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminBlogImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminBlogImageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.BlogImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogImageDto>.Fail("Blog görseli bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminBlogImageDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.BlogImages.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"BlogImage geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminBlogImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<BlogImageDto>> CreateAsync(CreateBlogImageDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Url))
            return Result<BlogImageDto>.Fail("Görsel URL'si boş olamaz.");

        var blogExists = await _unitOfWork.Blogs.AnyAsync(x => x.Id == dto.BlogId, cancellationToken);
        if (!blogExists)
            return Result<BlogImageDto>.Fail("İlgili blog bulunamadı.", statusCode: 404);

        var entity = dto.ToEntity();
        await _unitOfWork.BlogImages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"BlogImage oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<BlogImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<BlogImageDto>> UpdateAsync(UpdateBlogImageDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.BlogImages.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<BlogImageDto>.Fail("Blog görseli bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Url))
            return Result<BlogImageDto>.Fail("Görsel URL'si boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.BlogImages.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"BlogImage güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<BlogImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.BlogImages.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Blog görseli bulunamadı.", statusCode: 404);

        await _unitOfWork.BlogImages.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"BlogImage silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.BlogImages.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static Expression<Func<BlogImage, bool>>? AdminPredicate(AdminListQuery query, bool? isCover, int? blogId)
    {
        var predicate = AdminFilters.Common<BlogImage>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => (x.Url != null && x.Url.Contains(term)) || (x.AltText != null && x.AltText.Contains(term)));
        if (isCover is { } cover)
            predicate = predicate.AndAlso(x => x.IsCover == cover);
        if (blogId is { } id)
            predicate = predicate.AndAlso(x => x.BlogId == id);
        return predicate;
    }

    public async Task<PagedResult<AdminBlogImageDto>> GetAllForAdminPagedAsync(AdminListQuery query, bool? isCover, int? blogId, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query, isCover, blogId);
        var entities = await _unitOfWork.BlogImages.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken);
        var total = await _unitOfWork.BlogImages.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminBlogImageDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, bool? isCover, int? blogId, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.BlogImages.GetAdminStatusCountsAsync(AdminPredicate(query, isCover, blogId), cancellationToken));

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.BlogImages, action, ids, userId, "blog görseli", _activityLogger, cancellationToken);
}