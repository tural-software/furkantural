using FurkanTural_Domain.Entities;
using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Role;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class RoleService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IRoleService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<RoleDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<RoleDto>.Fail("Rol bulunamadı.", statusCode: 404);

        return Result<RoleDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<RoleDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        return Result<IEnumerable<RoleDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<RoleDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Roles.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Roles.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<RoleDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<RoleDto>.Fail("Rol adı boş olamaz.");

        var nameExists = await _unitOfWork.Roles.AnyAsync(x => x.Name == dto.Name, cancellationToken);
        if (nameExists)
            return Result<RoleDto>.Fail("Bu rol adı zaten kullanılıyor.");

        var entity = dto.ToEntity();
        await _unitOfWork.Roles.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Rol oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<RoleDto>.Ok(entity.ToDto());
    }

    public async Task<Result<RoleDto>> UpdateAsync(UpdateRoleDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Roles.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<RoleDto>.Fail("Rol bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<RoleDto>.Fail("Rol adı boş olamaz.");

        var nameExists = await _unitOfWork.Roles.AnyAsync(x => x.Name == dto.Name && x.Id != dto.Id, cancellationToken);
        if (nameExists)
            return Result<RoleDto>.Fail("Bu rol adı zaten kullanılıyor.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Roles.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Rol güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<RoleDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Rol bulunamadı.", statusCode: 404);

        await _unitOfWork.Roles.SoftDeleteAsync(entity, deletedBy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Rol silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminRoleDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Roles.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminRoleDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminRoleDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Roles.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminRoleDto>.Fail("Rol bulunamadı.", statusCode: 404);

        return Result<AdminRoleDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminRoleDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Roles.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminRoleDto>.Fail("Rol bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminRoleDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Roles.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Rol aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminRoleDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminRoleDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Roles.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminRoleDto>.Fail("Rol bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminRoleDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Roles.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Rol geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminRoleDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Roles.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private static Expression<Func<Role, bool>>? AdminPredicate(AdminListQuery query)
    {
        var predicate = AdminFilters.Common<Role>(query);
        if (query.SearchTerm is { } term)
            predicate = predicate.AndAlso(x => x.Name != null && x.Name.Contains(term));
        return predicate;
    }

    public async Task<PagedResult<AdminRoleDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default)
    {
        var predicate = AdminPredicate(query);
        var entities = await _unitOfWork.Roles.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, false, cancellationToken);
        var total = await _unitOfWork.Roles.CountForAdminAsync(predicate, cancellationToken);
        return PagedResult<AdminRoleDto>.Ok(entities.Select(e => e.ToAdminDto()), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.Roles.GetAdminStatusCountsAsync(AdminPredicate(query), cancellationToken));

    public async Task<Result<IReadOnlyList<AdminOptionDto>>> GetAdminOptionsAsync(string? search, int? take, CancellationToken cancellationToken = default)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        Expression<Func<Role, bool>>? predicate = term is null ? null : x => x.Name != null && x.Name.Contains(term);
        var options = await _unitOfWork.Roles.GetAdminOptionsAsync(predicate, x => x.Name, x => new AdminOptionDto(x.Id, x.Name ?? ""), take, cancellationToken);
        return Result<IReadOnlyList<AdminOptionDto>>.Ok(options.Select(o => o.Label.Length > 0 ? o : o with { Label = $"Rol #{o.Id}" }).ToList());
    }

    public Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default)
        => BulkActions.ApplyAsync(_unitOfWork, _unitOfWork.Roles, action, ids, userId, "rol", _activityLogger, cancellationToken);
}