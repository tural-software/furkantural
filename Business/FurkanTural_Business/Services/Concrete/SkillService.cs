using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Skill;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

public class SkillService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : ISkillService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<SkillDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<SkillDto>.Fail("Yetenek bulunamadı.", statusCode: 404);

        return Result<SkillDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<SkillDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Skills.GetAllAsync(cancellationToken);
        return Result<IEnumerable<SkillDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<SkillDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Skills.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Skills.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<SkillDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<SkillDto>> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<SkillDto>.Fail("Yetenek adı boş olamaz.");

        if (dto.Proficiency < 0 || dto.Proficiency > 100)
            return Result<SkillDto>.Fail("Yeterlilik değeri 0 ile 100 arasında olmalıdır.");

        var entity = dto.ToEntity();
        await _unitOfWork.Skills.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yetenek oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<SkillDto>.Ok(entity.ToDto());
    }

    public async Task<Result<SkillDto>> UpdateAsync(UpdateSkillDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<SkillDto>.Fail("Yetenek bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<SkillDto>.Fail("Yetenek adı boş olamaz.");

        if (dto.Proficiency < 0 || dto.Proficiency > 100)
            return Result<SkillDto>.Fail("Yeterlilik değeri 0 ile 100 arasında olmalıdır.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Skills.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yetenek güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<SkillDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Yetenek bulunamadı.", statusCode: 404);

        await _unitOfWork.Skills.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yetenek silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminSkillDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Skills.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminSkillDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminSkillDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminSkillDto>.Fail("Yetenek bulunamadı.", statusCode: 404);

        return Result<AdminSkillDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminSkillDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminSkillDto>.Fail("Yetenek bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminSkillDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Skills.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yetenek aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminSkillDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminSkillDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Skills.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminSkillDto>.Fail("Yetenek bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminSkillDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Skills.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Yetenek geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminSkillDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Skills.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static Expression<Func<Skill, bool>>? AdminPredicate(AdminListQuery query)
    {
        var predicate = AdminFilters.Common<Skill>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Name != null && x.Name.Contains(term));
        return predicate;
    }

    public async Task<PagedResult<AdminSkillDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query);
        var entities = await _unitOfWork.Skills.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Skills.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminSkillDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.Skills.GetAdminStatusCountsAsync(AdminPredicate(query), cancellationToken));

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.Skills, action, ids, userId, "beceri", _activityLogger, cancellationToken);
}