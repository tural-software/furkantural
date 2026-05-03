using FurkanTural_Application.DTOs.MusicImage;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class MusicImageService(IUnitOfWork unitOfWork) : IMusicImageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

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

        return Result<MusicImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MusicImages.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Müzik görseli bulunamadı.", statusCode: 404);

        await _unitOfWork.MusicImages.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}