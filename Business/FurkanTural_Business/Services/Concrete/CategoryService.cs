using System.Text.RegularExpressions;
using FurkanTural_Application.DTOs.Category;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public partial class CategoryService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexColorRegex();

    private static string? ValidateColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null; // renk opsiyonel
        return HexColorRegex().IsMatch(color.Trim())
            ? null
            : "Renk geçerli bir hex kodu olmalıdır (örn. #38bdf8).";
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<CategoryDto>.Fail("Kategori bulunamadı.", statusCode: 404);

        return Result<CategoryDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        return Result<IEnumerable<CategoryDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<PagedResult<CategoryDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Categories.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Categories.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<CategoryDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<CategoryDto>.Fail("Kategori adı boş olamaz.");

        if (ValidateColor(dto.Color) is { } colorError)
            return Result<CategoryDto>.Fail(colorError);

        var entity = dto.ToEntity();
        await _unitOfWork.Categories.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kategori oluşturuldu. Id: {entity.Id}", cancellationToken);

        return Result<CategoryDto>.Ok(entity.ToDto());
    }

    public async Task<Result<CategoryDto>> UpdateAsync(UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Categories.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<CategoryDto>.Fail("Kategori bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<CategoryDto>.Fail("Kategori adı boş olamaz.");

        if (ValidateColor(dto.Color) is { } colorError)
            return Result<CategoryDto>.Fail(colorError);

        entity.UpdateEntity(dto);
        await _unitOfWork.Categories.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kategori güncellendi. Id: {entity.Id}", cancellationToken);

        return Result<CategoryDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Kategori bulunamadı.", statusCode: 404);

        await _unitOfWork.Categories.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kategori silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AdminCategoryDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Categories.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminCategoryDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<Result<AdminCategoryDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Categories.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCategoryDto>.Fail("Kategori bulunamadı.", statusCode: 404);

        return Result<AdminCategoryDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminCategoryDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Categories.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCategoryDto>.Fail("Kategori bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminCategoryDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Categories.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kategori aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminCategoryDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminCategoryDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Categories.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCategoryDto>.Fail("Kategori bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminCategoryDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Categories.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kategori geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminCategoryDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Categories.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}
