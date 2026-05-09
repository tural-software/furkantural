using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Music;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class MusicService(IUnitOfWork unitOfWork) : IMusicService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

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

        return Result<MusicDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Musics.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Müzik bulunamadı.", statusCode: 404);

        await _unitOfWork.Musics.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
}