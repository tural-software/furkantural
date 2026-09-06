using FurkanTural_Domain.Entities;
using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Project;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class ProjectService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IProjectService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

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
        await _activityLogger.LogAsync($"Proje aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

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
        await _activityLogger.LogAsync($"Proje geri yüklendi. Id: {id}", cancellationToken);

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
        await _activityLogger.LogAsync($"Proje oluşturuldu. Id: {entity.Id}", cancellationToken);

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
        await _activityLogger.LogAsync($"Proje güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<ProjectDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Proje bulunamadı.", statusCode: 404);

        await _unitOfWork.Projects.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Proje silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Projects.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static Expression<Func<Project, bool>>? AdminPredicate(AdminListQuery query, bool? isCompleted, int? projectId)
    {
        var predicate = AdminFilters.Common<Project>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Title != null && x.Title.Contains(term));
        if (isCompleted is { } completed)
            predicate = predicate.AndAlso(x => x.IsCompleted == completed);
        if (projectId is { } id)
            predicate = predicate.AndAlso(x => x.Id == id);
        return predicate;
    }

    public async Task<PagedResult<AdminProjectDto>> GetAllForAdminPagedAsync(AdminListQuery query, bool? isCompleted, int? projectId, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query, isCompleted, projectId);
        var entities = await _unitOfWork.Projects.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken);
        var total = await _unitOfWork.Projects.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminProjectDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, bool? isCompleted, int? projectId, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.Projects.GetAdminStatusCountsAsync(AdminPredicate(query, isCompleted, projectId), cancellationToken));

    public async Task<Result<IReadOnlyList<AdminOptionDto>>> GetAdminOptionsAsync(string? search, int? take, CancellationToken cancellationToken = default)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        Expression<Func<Project, bool>>? predicate = term is null ? null : x => x.Title != null && x.Title.Contains(term);
        var options = await _unitOfWork.Projects.GetAdminOptionsAsync(predicate, x => x.Title, x => new AdminOptionDto(x.Id, x.Title ?? ""), take, cancellationToken);
        return Result<IReadOnlyList<AdminOptionDto>>.Ok(options.Select(o => o.Label.Length > 0 ? o : o with { Label = $"Proje #{o.Id}" }).ToList());
    }

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.Projects, action, ids, userId, "proje", _activityLogger, cancellationToken);
}