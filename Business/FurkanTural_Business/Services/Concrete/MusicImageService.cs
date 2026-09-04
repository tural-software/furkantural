using FurkanTural_Domain.Entities;
using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.MusicImage;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class MusicImageService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IMusicImageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<MusicImageDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MusicImages.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<MusicImageDto>.Fail("Müzik görseli bulunamadı.", statusCode: 404);

        return Result<MusicImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<MusicImageDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MusicImages.GetAllAsync(cancellationToken);
        return Result<IEnumerable<MusicImageDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<MusicImageDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MusicImages.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.MusicImages.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<MusicImageDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<IEnumerable<MusicImageDto>>> GetByMusicIdAsync(int musicId, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MusicImages.GetAllAsync(x => x.MusicId == musicId, cancellationToken);
        return Result<IEnumerable<MusicImageDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<Result<MusicImageDto>> CreateAsync(CreateMusicImageDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Url))
            return Result<MusicImageDto>.Fail("Görsel URL'si boş olamaz.");

        var musicExists = await _unitOfWork.Musics.AnyAsync(x => x.Id == dto.MusicId, cancellationToken);
        if (!musicExists)
            return Result<MusicImageDto>.Fail("İlgili müzik bulunamadı.", statusCode: 404);

        var entity = dto.ToEntity();
        await _unitOfWork.MusicImages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"MusicImage oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<MusicImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<MusicImageDto>> UpdateAsync(UpdateMusicImageDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MusicImages.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<MusicImageDto>.Fail("Müzik görseli bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Url))
            return Result<MusicImageDto>.Fail("Görsel URL'si boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.MusicImages.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"MusicImage güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<MusicImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MusicImages.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Müzik görseli bulunamadı.", statusCode: 404);

        await _unitOfWork.MusicImages.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"MusicImage silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<AdminMusicImageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MusicImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMusicImageDto>.Fail("Müzik görseli bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminMusicImageDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.MusicImages.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"MusicImage geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminMusicImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminMusicImageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MusicImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMusicImageDto>.Fail("Müzik görseli bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminMusicImageDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.MusicImages.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"MusicImage aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminMusicImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<IEnumerable<AdminMusicImageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MusicImages.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminMusicImageDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminMusicImageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MusicImages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMusicImageDto>.Fail("Müzik görseli bulunamadı.", statusCode: 404);

        return Result<AdminMusicImageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.MusicImages.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static Expression<Func<MusicImage, bool>>? AdminPredicate(AdminListQuery query, bool? isCover, int? musicId)
    {
        var predicate = AdminFilters.Common<MusicImage>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Url != null && x.Url.Contains(term));
        if (isCover is { } cover)
            predicate = predicate.AndAlso(x => x.IsCover == cover);
        if (musicId is { } id)
            predicate = predicate.AndAlso(x => x.MusicId == id);
        return predicate;
    }

    public async Task<PagedResult<AdminMusicImageDto>> GetAllForAdminPagedAsync(AdminListQuery query, bool? isCover, int? musicId, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query, isCover, musicId);
        var entities = await _unitOfWork.MusicImages.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken);
        var total = await _unitOfWork.MusicImages.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminMusicImageDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, bool? isCover, int? musicId, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.MusicImages.GetAdminStatusCountsAsync(AdminPredicate(query, isCover, musicId), cancellationToken));
}