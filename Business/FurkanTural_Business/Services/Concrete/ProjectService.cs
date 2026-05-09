using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Project;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class ProjectService(IUnitOfWork unitOfWork) : IProjectService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<ProjectDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<ProjectDto>.Fail("Proje bulunamadı.", statusCode: 404);

        return Result<ProjectDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<ProjectDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Projects.GetAllAsync(cancellationToken);
        return Result<IEnumerable<ProjectDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<Result<IEnumerable<AdminProjectDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Projects.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminProjectDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminProjectDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Projects.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminProjectDto>.Fail("Proje bulunamadı.", statusCode: 404);

        return Result<AdminProjectDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminProjectDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Projects.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminProjectDto>.Fail("Proje bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminProjectDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Projects.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminProjectDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminProjectDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Projects.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminProjectDto>.Fail("Proje bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminProjectDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Projects.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminProjectDto>.Ok(entity.ToAdminDto());
    }

    public async Task<PagedResult<ProjectDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Projects.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Projects.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<ProjectDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<ProjectDto>.Fail("Başlık boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Description))
            return Result<ProjectDto>.Fail("Açıklama boş olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.Projects.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProjectDto>.Ok(entity.ToDto());
    }

    public async Task<Result<ProjectDto>> UpdateAsync(UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Projects.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<ProjectDto>.Fail("Proje bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<ProjectDto>.Fail("Başlık boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Description))
            return Result<ProjectDto>.Fail("Açıklama boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Projects.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProjectDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Proje bulunamadı.", statusCode: 404);

        await _unitOfWork.Projects.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Projects.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}
