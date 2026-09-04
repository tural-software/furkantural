using FurkanTural_Domain.Entities;
using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Music;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class MusicService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IMusicService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<MusicDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Musics.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<MusicDto>.Fail("Müzik bulunamadı.", statusCode: 404);

        return Result<MusicDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<MusicDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Musics.GetAllAsync(cancellationToken);
        return Result<IEnumerable<MusicDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<MusicDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Musics.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Musics.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<MusicDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<MusicDto>> CreateAsync(CreateMusicDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<MusicDto>.Fail("Müzik adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Artist))
            return Result<MusicDto>.Fail("Sanatçı adı boş olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.Musics.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Müzik oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<MusicDto>.Ok(entity.ToDto());
    }

    public async Task<Result<MusicDto>> UpdateAsync(UpdateMusicDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Musics.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<MusicDto>.Fail("Müzik bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<MusicDto>.Fail("Müzik adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Artist))
            return Result<MusicDto>.Fail("Sanatçı adı boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Musics.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Müzik güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<MusicDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Musics.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Müzik bulunamadı.", statusCode: 404);

        await _unitOfWork.Musics.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Müzik silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<AdminMusicDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Musics.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMusicDto>.Fail("Müzik bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminMusicDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Musics.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Müzik geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminMusicDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminMusicDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Musics.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMusicDto>.Fail("Müzik bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminMusicDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Musics.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Müzik aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminMusicDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<IEnumerable<AdminMusicDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Musics.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminMusicDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminMusicDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Musics.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMusicDto>.Fail("Müzik bulunamadı.", statusCode: 404);

        return Result<AdminMusicDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Musics.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static Expression<Func<Music, bool>>? AdminPredicate(AdminListQuery query, string? artist, int? musicId)
    {
        var predicate = AdminFilters.Common<Music>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Name != null && x.Name.Contains(term));
        if (!string.IsNullOrWhiteSpace(artist))
        {
            var artistTerm = artist.Trim();
            predicate = predicate.AndAlso(x => x.Artist != null && x.Artist.Contains(artistTerm));
        }
        if (musicId is { } id)
            predicate = predicate.AndAlso(x => x.Id == id);
        return predicate;
    }

    public async Task<PagedResult<AdminMusicDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? artist, int? musicId, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query, artist, musicId);
        var entities = await _unitOfWork.Musics.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken);
        var total = await _unitOfWork.Musics.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminMusicDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? artist, int? musicId, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.Musics.GetAdminStatusCountsAsync(AdminPredicate(query, artist, musicId), cancellationToken));

    public async Task<Result<IReadOnlyList<AdminOptionDto>>> GetAdminOptionsAsync(string? search, int? take, CancellationToken cancellationToken = default)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        Expression<Func<Music, bool>>? predicate = term is null ? null : x => x.Name != null && x.Name.Contains(term);
        var options = await _unitOfWork.Musics.GetAdminOptionsAsync(predicate, x => x.Name, x => new AdminOptionDto(x.Id, x.Name ?? ""), take, cancellationToken);
        return Result<IReadOnlyList<AdminOptionDto>>.Ok(options.Select(o => o.Label.Length > 0 ? o : o with { Label = $"Müzik #{o.Id}" }).ToList());
    }
}