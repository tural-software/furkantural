using FurkanTural_Domain.Entities;
using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.ProjectImage;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class ProjectImageService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IProjectImageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<ProjectImageDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ProjectImages.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<ProjectImageDto>.Fail("Proje görseli bulunamadı.", statusCode: 404);

        return Result<ProjectImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<ProjectImageDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ProjectImages.GetAllAsync(cancellationToken);
        return Result<IEnumerable<ProjectImageDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<ProjectImageDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ProjectImages.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.ProjectImages.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<ProjectImageDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<IEnumerable<ProjectImageDto>>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ProjectImages.GetAllAsync(x => x.ProjectId == projectId, cancellationToken);
        return Result<IEnumerable<ProjectImageDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<Result<IEnumerable<AdminProjectImageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ProjectImages.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminProjectImageDto>>.Ok(entities.Select(x => x.ToAdminDto()));
    }

    public async Task<Result<AdminProjectImageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ProjectImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminProjectImageDto>.Fail("Proje görseli bulunamadı.", statusCode: 404);

        return Result<AdminProjectImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminProjectImageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ProjectImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminProjectImageDto>.Fail("Proje görseli bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminProjectImageDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.ProjectImages.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"ProjectImage aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminProjectImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminProjectImageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ProjectImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminProjectImageDto>.Fail("Proje görseli bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminProjectImageDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.ProjectImages.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"ProjectImage geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminProjectImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<ProjectImageDto>> CreateAsync(CreateProjectImageDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Url))
            return Result<ProjectImageDto>.Fail("Görsel URL'si boş olamaz.");

        var projectExists = await _unitOfWork.Projects.AnyAsync(x => x.Id == dto.ProjectId, cancellationToken);
        if (!projectExists)
            return Result<ProjectImageDto>.Fail("İlgili proje bulunamadı.", statusCode: 404);

        var entity = dto.ToEntity();
        await _unitOfWork.ProjectImages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"ProjectImage oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<ProjectImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<ProjectImageDto>> UpdateAsync(UpdateProjectImageDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ProjectImages.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<ProjectImageDto>.Fail("Proje görseli bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Url))
            return Result<ProjectImageDto>.Fail("Görsel URL'si boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.ProjectImages.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"ProjectImage güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<ProjectImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ProjectImages.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Proje görseli bulunamadı.", statusCode: 404);

        await _unitOfWork.ProjectImages.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"ProjectImage silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.ProjectImages.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static Expression<Func<ProjectImage, bool>>? AdminPredicate(AdminListQuery query, bool? isCover, int? projectId)
    {
        var predicate = AdminFilters.Common<ProjectImage>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Url != null && x.Url.Contains(term));
        if (isCover is { } cover)
            predicate = predicate.AndAlso(x => x.IsCover == cover);
        if (projectId is { } id)
            predicate = predicate.AndAlso(x => x.ProjectId == id);
        return predicate;
    }

    public async Task<PagedResult<AdminProjectImageDto>> GetAllForAdminPagedAsync(AdminListQuery query, bool? isCover, int? projectId, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query, isCover, projectId);
        var entities = await _unitOfWork.ProjectImages.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken);
        var total = await _unitOfWork.ProjectImages.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminProjectImageDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, bool? isCover, int? projectId, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.ProjectImages.GetAdminStatusCountsAsync(AdminPredicate(query, isCover, projectId), cancellationToken));

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.ProjectImages, action, ids, userId, "proje görseli", _activityLogger, cancellationToken);
}