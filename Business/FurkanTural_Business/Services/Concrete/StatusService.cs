using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Status;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class StatusService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IStatusService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<StatusDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Statuses.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<StatusDto>.Fail("Statü bulunamadı.", statusCode: 404);

        return Result<StatusDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<StatusDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Statuses.GetAllAsync(cancellationToken);
        return Result<IEnumerable<StatusDto>>.Ok(entities.OrderBy(e => e.Group).ThenBy(e => e.SortOrder).Select(e => e.ToDto()));
    }

    public async Task<PagedResult<StatusDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Statuses.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Statuses.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<StatusDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<IEnumerable<StatusDto>>> GetByGroupAsync(string group, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(group))
            return Result<IEnumerable<StatusDto>>.Fail("Grup boş olamaz.");

        var entities = await _unitOfWork.Statuses.GetAllAsync(x => x.Group == group, cancellationToken);
        return Result<IEnumerable<StatusDto>>.Ok(entities.OrderBy(e => e.SortOrder).Select(e => e.ToDto()));
    }

    public async Task<int?> GetIdByCodeAsync(string group, string code, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Statuses.GetAsync(x => x.Group == group && x.Code == code, cancellationToken);
        return entity?.Id;
    }

    public async Task<Result<StatusDto>> CreateAsync(CreateStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Group))
            return Result<StatusDto>.Fail("Grup boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Code))
            return Result<StatusDto>.Fail("Kod boş olamaz.");

        var exists = await _unitOfWork.Statuses.AnyAsync(x => x.Group == dto.Group && x.Code == dto.Code, cancellationToken);
        if (exists)
            return Result<StatusDto>.Fail("Bu grup için aynı kod zaten mevcut.");

        var entity = dto.ToEntity();
        await _unitOfWork.Statuses.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Statü oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<StatusDto>.Ok(entity.ToDto());
    }

    public async Task<Result<StatusDto>> UpdateAsync(UpdateStatusDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Statuses.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<StatusDto>.Fail("Statü bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Group))
            return Result<StatusDto>.Fail("Grup boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Code))
            return Result<StatusDto>.Fail("Kod boş olamaz.");

        var exists = await _unitOfWork.Statuses.AnyAsync(x => x.Group == dto.Group && x.Code == dto.Code && x.Id != dto.Id, cancellationToken);
        if (exists)
            return Result<StatusDto>.Fail("Bu grup için aynı kod zaten mevcut.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Statuses.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Statü güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<StatusDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Statuses.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Statü bulunamadı.", statusCode: 404);

        await _unitOfWork.Statuses.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Statü silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminStatusDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Statuses.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminStatusDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminStatusDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Statuses.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminStatusDto>.Fail("Statü bulunamadı.", statusCode: 404);

        return Result<AdminStatusDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminStatusDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Statuses.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminStatusDto>.Fail("Statü bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminStatusDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Statuses.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Statü aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminStatusDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminStatusDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Statuses.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminStatusDto>.Fail("Statü bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminStatusDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Statuses.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Statü geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminStatusDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Statuses.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}