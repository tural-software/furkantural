using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Subscriber;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Abonelik akışının varlık kontrolü global süzgeci atlar (bkz. <see cref="ISubscriberRepository"/>); Email tekil indeksi yumuşak silmeye göre süzülmediği için abonelikten çıkmış bir adres indekste hâlâ tutuludur ve süzgeçli bir kontrol onu göremez.<para>Aynı adresle yeniden abone olmak reddedilmez, duran satır geri açılır. Abonelik bir hesap değildir: arkasında parola, veri ya da kimlik yoktur, dolayısıyla hesap akışındaki posta doğrulaması burada karşılıksızdır — geri açmak ilk kez abone olmakla aynı şeydir ve yanıtı da aynıdır.</para><para>Yönetim uçları aynı kontrolü yapar ama sonucu saklamaz: admin zaten tabloyu görebildiği için çakışan kaydın kimliği yanıta yazılır, böylece "yeni kayıt mı açayım, duranı mı geri yükleyeyim" sorusu panelden cevaplanabilir.</para></summary>

public class SubscriberService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : ISubscriberService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<SubscriberDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<SubscriberDto>.Fail("Abone bulunamadı.", statusCode: 404);

        return Result<SubscriberDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<SubscriberDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Subscribers.GetAllAsync(cancellationToken);
        return Result<IEnumerable<SubscriberDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<SubscriberDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Subscribers.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Subscribers.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<SubscriberDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<SubscriberDto>> CreateAsync(CreateSubscriberDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<SubscriberDto>.Fail("E-posta adresi boş olamaz.");

        var owner = await _unitOfWork.Subscribers.GetByEmailForAdminAsync(dto.Email, cancellationToken);
        if (owner is not null)
            return Result<SubscriberDto>.Fail(OccupiedMessage(owner), statusCode: 409);

        var entity = dto.ToEntity();
        await _unitOfWork.Subscribers.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Abone oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<SubscriberDto>.Ok(entity.ToDto());
    }

    public async Task<Result<SubscriberDto>> UpdateAsync(UpdateSubscriberDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<SubscriberDto>.Fail("Abone bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<SubscriberDto>.Fail("E-posta adresi boş olamaz.");

        var owner = await _unitOfWork.Subscribers.GetByEmailForAdminAsync(dto.Email, cancellationToken);
        if (owner is not null && owner.Id != entity.Id)
            return Result<SubscriberDto>.Fail(OccupiedMessage(owner), statusCode: 409);

        entity.UpdateEntity(dto);
        await _unitOfWork.Subscribers.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Abone güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<SubscriberDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Abone bulunamadı.", statusCode: 404);

        await _unitOfWork.Subscribers.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Abone silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminSubscriberDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Subscribers.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminSubscriberDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminSubscriberDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminSubscriberDto>.Fail("Abone bulunamadı.", statusCode: 404);

        return Result<AdminSubscriberDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminSubscriberDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminSubscriberDto>.Fail("Abone bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminSubscriberDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Subscribers.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Abone aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminSubscriberDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result> SubscribeAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail("E-posta adresi boş olamaz.");

        var existing = await _unitOfWork.Subscribers.GetByEmailForAdminAsync(email, cancellationToken);

        if (existing is { IsDeleted: false, IsActive: true })
            return Result.Fail("Bu e-posta adresi zaten abone listesinde.");

        if (existing is not null)
        {
            await _unitOfWork.Subscribers.RestoreAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _activityLogger.LogAsync($"Abonelik yeniden açıldı. Id: {existing.Id}", cancellationToken);

            return Result.Ok("Abonelik başarıyla tamamlandı.");
        }

        var entity = new CreateSubscriberDto { Email = email }.ToEntity();
        await _unitOfWork.Subscribers.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Abone olundu. Email: {email}", cancellationToken);

        return Result.Ok("Abonelik başarıyla tamamlandı.");
    }

    public async Task<Result> UnsubscribeAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail("E-posta adresi boş olamaz.");

        var entity = await _unitOfWork.Subscribers.GetAsync(x => x.Email == email, cancellationToken);
        if (entity is null)
            return Result.Fail("Bu e-posta adresi abone listesinde bulunamadı.", statusCode: 404);

        await _unitOfWork.Subscribers.SoftDeleteAsync(entity, null, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Abonelik iptal edildi. Email: {email}", cancellationToken);

        return Result.Ok("Abonelik başarıyla iptal edildi.");
    }

    public async Task<Result<AdminSubscriberDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subscribers.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminSubscriberDto>.Fail("Abone bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminSubscriberDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Subscribers.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Abone geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminSubscriberDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Subscribers.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static string OccupiedMessage(Subscriber owner)
        => owner.IsDeleted
            ? $"Bu e-posta adresi silinmiş bir kayıtta duruyor (#{owner.Id}); yeni kayıt açmak yerine onu geri yükleyin."
            : $"Bu e-posta adresi zaten kayıtlı (#{owner.Id}).";

    private static Expression<Func<Subscriber, bool>>? AdminPredicate(AdminListQuery query)
    {
        var predicate = AdminFilters.Common<Subscriber>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Email != null && x.Email.Contains(term));
        return predicate;
    }

    public async Task<PagedResult<AdminSubscriberDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query);
        var entities = await _unitOfWork.Subscribers.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken);
        var total = await _unitOfWork.Subscribers.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminSubscriberDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.Subscribers.GetAdminStatusCountsAsync(AdminPredicate(query), cancellationToken));

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.Subscribers, action, ids, userId, "abone", _activityLogger, cancellationToken);
}
