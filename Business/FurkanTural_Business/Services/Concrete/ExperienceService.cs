using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Experience;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class ExperienceService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IExperienceService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<ExperienceDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Experiences.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<ExperienceDto>.Fail("Tecrübe bilgisi bulunamadı.", statusCode: 404);

        return Result<ExperienceDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<ExperienceDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Experiences.GetAllAsync(cancellationToken);
        return Result<IEnumerable<ExperienceDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<ExperienceDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Experiences.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Experiences.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<ExperienceDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<ExperienceDto>> CreateAsync(CreateExperienceDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Position))
            return Result<ExperienceDto>.Fail("Pozisyon boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            return Result<ExperienceDto>.Fail("Firma adı boş olamaz.");

        if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
            return Result<ExperienceDto>.Fail("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.Experiences.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Tecrübe oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<ExperienceDto>.Ok(entity.ToDto());
    }

    public async Task<Result<ExperienceDto>> UpdateAsync(UpdateExperienceDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Experiences.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<ExperienceDto>.Fail("Tecrübe bilgisi bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Position))
            return Result<ExperienceDto>.Fail("Pozisyon boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            return Result<ExperienceDto>.Fail("Firma adı boş olamaz.");

        if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
            return Result<ExperienceDto>.Fail("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Experiences.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Tecrübe güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<ExperienceDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Experiences.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Tecrübe bilgisi bulunamadı.", statusCode: 404);

        await _unitOfWork.Experiences.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Tecrübe silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminExperienceDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Experiences.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminExperienceDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminExperienceDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Experiences.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminExperienceDto>.Fail("Tecrübe bilgisi bulunamadı.", statusCode: 404);

        return Result<AdminExperienceDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminExperienceDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Experiences.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminExperienceDto>.Fail("Tecrübe bilgisi bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminExperienceDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Experiences.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Tecrübe aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminExperienceDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminExperienceDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Experiences.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminExperienceDto>.Fail("Tecrübe bilgisi bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminExperienceDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Experiences.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Tecrübe geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminExperienceDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Experiences.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}