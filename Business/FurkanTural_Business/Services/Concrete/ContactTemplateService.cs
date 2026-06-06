using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.ContactTemplate;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class ContactTemplateService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IContactTemplateService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<ContactTemplateDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ContactTemplates.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<ContactTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);
        return Result<ContactTemplateDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<ContactTemplateDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ContactTemplates.GetAllAsync(cancellationToken);
        return Result<IEnumerable<ContactTemplateDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<ContactTemplateDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ContactTemplates.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.ContactTemplates.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<ContactTemplateDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<ContactTemplateDto>> CreateAsync(CreateContactTemplateDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<ContactTemplateDto>.Fail("Şablon adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(dto.TemplateType))
            return Result<ContactTemplateDto>.Fail("Şablon tipi boş olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.ContactTemplates.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"İletişim şablonu oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<ContactTemplateDto>.Ok(entity.ToDto());
    }

    public async Task<Result<ContactTemplateDto>> UpdateAsync(UpdateContactTemplateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ContactTemplates.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<ContactTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<ContactTemplateDto>.Fail("Şablon adı boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.ContactTemplates.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"İletişim şablonu güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<ContactTemplateDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ContactTemplates.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Şablon bulunamadı.", statusCode: 404);

        await _unitOfWork.ContactTemplates.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"İletişim şablonu silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminContactTemplateDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ContactTemplates.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminContactTemplateDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminContactTemplateDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ContactTemplates.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminContactTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);
        return Result<AdminContactTemplateDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminContactTemplateDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ContactTemplates.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminContactTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminContactTemplateDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.ContactTemplates.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminContactTemplateDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminContactTemplateDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ContactTemplates.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminContactTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminContactTemplateDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.ContactTemplates.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminContactTemplateDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.ContactTemplates.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}
