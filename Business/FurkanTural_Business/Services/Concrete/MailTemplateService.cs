using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.MailTemplate;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Tür ve proje bilgisi listelerde tek okumayla toplanıp sözlükten dağıtılır; şablon başına ayrı sorgu açmamak içindir. Yönetim okumaları ikisini de filtresiz alır, aksi hâlde pasife alınmış bir türe ya da projeye bağlı şablon adsız görünürdü.<para>Projesi boş bırakılan şablon geçerlidir ve tüm projeler için geçerli genel sürüm anlamına gelir; yalnızca verilen bir proje kimliğinin karşılığı aranır.</para><para>Etkinlik değişimi kendi kısıtını taşımaz: "tür ve proje çifti başına tek etkin şablon" kuralı veri tabanındadır, dolayısıyla ikinci bir şablonu açma denemesi burada değil kaydetme anında reddedilir ve çakışma yanıtına dönüşür.</para></summary>
public class MailTemplateService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IMailTemplateService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    private async Task<Dictionary<int, MailTemplateType>> TypeMapAsync(bool forAdmin, CancellationToken cancellationToken)
    {
        var types = forAdmin
            ? await _unitOfWork.MailTemplateTypes.GetAllForAdminAsync(cancellationToken)
            : await _unitOfWork.MailTemplateTypes.GetAllAsync(cancellationToken);
        return types.ToDictionary(t => t.Id);
    }

    private static MailTemplateType? Lookup(Dictionary<int, MailTemplateType> map, int id)
        => map.TryGetValue(id, out var type) ? type : null;

    private async Task<Dictionary<int, AppSource>> AppSourceMapAsync(bool forAdmin, CancellationToken cancellationToken)
    {
        var sources = forAdmin
            ? await _unitOfWork.AppSources.GetAllForAdminAsync(cancellationToken)
            : await _unitOfWork.AppSources.GetAllAsync(cancellationToken);
        return sources.ToDictionary(s => s.Id);
    }

    private static AppSource? LookupApp(Dictionary<int, AppSource> map, int? id)
        => id is not null && map.TryGetValue(id.Value, out var source) ? source : null;

    public async Task<Result<MailTemplateDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplates.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<MailTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);

        var map = await TypeMapAsync(false, cancellationToken);
        var appMap = await AppSourceMapAsync(false, cancellationToken);
        return Result<MailTemplateDto>.Ok(entity.ToDto(Lookup(map, entity.MailTemplateTypeId), LookupApp(appMap, entity.AppSourceId)));
    }

    public async Task<Result<IEnumerable<MailTemplateDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MailTemplates.GetAllAsync(cancellationToken);
        var map = await TypeMapAsync(false, cancellationToken);
        var appMap = await AppSourceMapAsync(false, cancellationToken);
        return Result<IEnumerable<MailTemplateDto>>.Ok(entities.Select(e => e.ToDto(Lookup(map, e.MailTemplateTypeId), LookupApp(appMap, e.AppSourceId))));
    }

    public async Task<PagedResult<MailTemplateDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MailTemplates.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.MailTemplates.CountAsync(cancellationToken: cancellationToken);
        var map = await TypeMapAsync(false, cancellationToken);
        var appMap = await AppSourceMapAsync(false, cancellationToken);
        return PagedResult<MailTemplateDto>.Ok(
            entities.Select(e => e.ToDto(Lookup(map, e.MailTemplateTypeId), LookupApp(appMap, e.AppSourceId))), total, pageNumber, pageSize);
    }

    public async Task<Result<MailTemplateDto>> CreateAsync(CreateMailTemplateDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<MailTemplateDto>.Fail("Şablon adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(dto.Subject))
            return Result<MailTemplateDto>.Fail("Konu boş olamaz.");

        var type = await _unitOfWork.MailTemplateTypes.GetByIdAsync(dto.MailTemplateTypeId, cancellationToken);
        if (type is null)
            return Result<MailTemplateDto>.Fail("Posta türü bulunamadı.", statusCode: 404);

        AppSource? appSource = null;
        if (dto.AppSourceId is not null)
        {
            appSource = await _unitOfWork.AppSources.GetByIdAsync(dto.AppSourceId.Value, cancellationToken);
            if (appSource is null)
                return Result<MailTemplateDto>.Fail("Proje bulunamadı.", statusCode: 404);
        }

        var entity = dto.ToEntity();
        await _unitOfWork.MailTemplates.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Posta şablonu oluşturuldu. Id: {entity.Id}, Tür: {type.Code}, Proje: {appSource?.Code ?? "genel"}", cancellationToken);

        return Result<MailTemplateDto>.Ok(entity.ToDto(type, appSource));
    }

    public async Task<Result<MailTemplateDto>> UpdateAsync(UpdateMailTemplateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplates.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<MailTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<MailTemplateDto>.Fail("Şablon adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(dto.Subject))
            return Result<MailTemplateDto>.Fail("Konu boş olamaz.");

        var type = await _unitOfWork.MailTemplateTypes.GetByIdAsync(dto.MailTemplateTypeId, cancellationToken);
        if (type is null)
            return Result<MailTemplateDto>.Fail("Posta türü bulunamadı.", statusCode: 404);

        AppSource? appSource = null;
        if (dto.AppSourceId is not null)
        {
            appSource = await _unitOfWork.AppSources.GetByIdAsync(dto.AppSourceId.Value, cancellationToken);
            if (appSource is null)
                return Result<MailTemplateDto>.Fail("Proje bulunamadı.", statusCode: 404);
        }

        entity.UpdateEntity(dto);
        await _unitOfWork.MailTemplates.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Posta şablonu güncellendi. Id: {entity.Id}, Proje: {appSource?.Code ?? "genel"}", cancellationToken);

        return Result<MailTemplateDto>.Ok(entity.ToDto(type, appSource));
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplates.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Şablon bulunamadı.", statusCode: 404);

        await _unitOfWork.MailTemplates.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Posta şablonu silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminMailTemplateDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.MailTemplates.GetAllForAdminAsync(cancellationToken);
        var map = await TypeMapAsync(true, cancellationToken);
        var appMap = await AppSourceMapAsync(true, cancellationToken);
        return Result<IEnumerable<AdminMailTemplateDto>>.Ok(
            entities.Select(e => e.ToAdminDto(Lookup(map, e.MailTemplateTypeId), LookupApp(appMap, e.AppSourceId))));
    }

    public async Task<Result<AdminMailTemplateDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplates.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMailTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);

        var map = await TypeMapAsync(true, cancellationToken);
        var appMap = await AppSourceMapAsync(true, cancellationToken);
        return Result<AdminMailTemplateDto>.Ok(entity.ToAdminDto(Lookup(map, entity.MailTemplateTypeId), LookupApp(appMap, entity.AppSourceId)));
    }

    public async Task<Result<AdminMailTemplateDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplates.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMailTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminMailTemplateDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.MailTemplates.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var map = await TypeMapAsync(true, cancellationToken);
        var appMap = await AppSourceMapAsync(true, cancellationToken);
        return Result<AdminMailTemplateDto>.Ok(entity.ToAdminDto(Lookup(map, entity.MailTemplateTypeId), LookupApp(appMap, entity.AppSourceId)));
    }

    public async Task<Result<AdminMailTemplateDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.MailTemplates.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminMailTemplateDto>.Fail("Şablon bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminMailTemplateDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.MailTemplates.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var map = await TypeMapAsync(true, cancellationToken);
        var appMap = await AppSourceMapAsync(true, cancellationToken);
        return Result<AdminMailTemplateDto>.Ok(entity.ToAdminDto(Lookup(map, entity.MailTemplateTypeId), LookupApp(appMap, entity.AppSourceId)));
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.MailTemplates.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static Expression<Func<MailTemplate, bool>>? AdminPredicate(AdminListQuery query)
    {
        var predicate = AdminFilters.Common<MailTemplate>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Name != null && x.Name.Contains(term));
        return predicate;
    }

    public async Task<PagedResult<AdminMailTemplateDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query);
        var entities = await _unitOfWork.MailTemplates.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken);
        var total = await _unitOfWork.MailTemplates.CountForAdminAsync(predicate, cancellationToken);
        var map = await TypeMapAsync(true, cancellationToken);
        var appMap = await AppSourceMapAsync(true, cancellationToken);
        return PagedResult<AdminMailTemplateDto>.Ok(
            entities.Select(e => e.ToAdminDto(Lookup(map, e.MailTemplateTypeId), LookupApp(appMap, e.AppSourceId))),
            total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.MailTemplates.GetAdminStatusCountsAsync(AdminPredicate(query), cancellationToken));

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.MailTemplates, action, ids, userId, "posta şablonu", _activityLogger, cancellationToken);
}
