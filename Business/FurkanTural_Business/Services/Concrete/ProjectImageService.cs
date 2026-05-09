using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.ProjectImage;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class ProjectImageService(IUnitOfWork unitOfWork) : IProjectImageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

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

        return Result<ProjectImageDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ProjectImages.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Proje görseli bulunamadı.", statusCode: 404);

        await _unitOfWork.ProjectImages.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.ProjectImages.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}
