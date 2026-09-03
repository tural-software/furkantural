using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.MailTemplateType;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Kod tekilliği veri tabanındaki indekste durur, buradaki ön denetim yalnızca hızlı ve okunur bir ret üretmek içindir; yarış hâlinde son sözü indeks söyler ve çakışma yanıta öyle dönüşür.<para>Silme yumuşaktır ama şablonu duran bir türü silmek onu gönderim yolundan düşürür: okuma küresel süzgeçten geçtiği için pasif ya da silinmiş tür bulunamaz ve o türün postası gönderilemez hâle gelir.</para></summary>
public class MailTemplateTypeService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IMailTemplateTypeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<MailTemplateTypeDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplateTypes.GetByIdAsync(id, cancellationToken);
        return entity is null
            ? Result<MailTemplateTypeDto>.Fail("Posta türü bulunamadı.", statusCode: 404)
            : Result<MailTemplateTypeDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<MailTemplateTypeDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MailTemplateTypes.GetAllAsync(cancellationToken);
        return Result<IEnumerable<MailTemplateTypeDto>>.Ok(entities.OrderBy(e => e.SortOrder).Select(e => e.ToDto()));
    }

    public async Task<PagedResult<MailTemplateTypeDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MailTemplateTypes.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.MailTemplateTypes.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<MailTemplateTypeDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<MailTemplateTypeDto>> CreateAsync(CreateMailTemplateTypeDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return Result<MailTemplateTypeDto>.Fail("Tür kodu boş olamaz.");
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<MailTemplateTypeDto>.Fail("Tür adı boş olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.MailTemplateTypes.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Posta türü oluşturuldu. Id: {entity.Id}, Kod: {entity.Code}", cancellationToken);

        return Result<MailTemplateTypeDto>.Ok(entity.ToDto());
    }

    public async Task<Result<MailTemplateTypeDto>> UpdateAsync(UpdateMailTemplateTypeDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplateTypes.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<MailTemplateTypeDto>.Fail("Posta türü bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Code))
            return Result<MailTemplateTypeDto>.Fail("Tür kodu boş olamaz.");
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<MailTemplateTypeDto>.Fail("Tür adı boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.MailTemplateTypes.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Posta türü güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<MailTemplateTypeDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplateTypes.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Posta türü bulunamadı.", statusCode: 404);

        await _unitOfWork.MailTemplateTypes.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Posta türü silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminMailTemplateTypeDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MailTemplateTypes.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminMailTemplateTypeDto>>.Ok(entities.OrderBy(e => e.SortOrder).Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminMailTemplateTypeDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplateTypes.GetByIdForAdminAsync(id, cancellationToken);
        return entity is null
            ? Result<AdminMailTemplateTypeDto>.Fail("Posta türü bulunamadı.", statusCode: 404)
            : Result<AdminMailTemplateTypeDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminMailTemplateTypeDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplateTypes.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMailTemplateTypeDto>.Fail("Posta türü bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminMailTemplateTypeDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.MailTemplateTypes.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminMailTemplateTypeDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminMailTemplateTypeDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplateTypes.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMailTemplateTypeDto>.Fail("Posta türü bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminMailTemplateTypeDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.MailTemplateTypes.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminMailTemplateTypeDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.MailTemplateTypes.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}
