using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Application.DTOs.Category;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class BlogService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IBlogService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result<BlogDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<BlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        var dto = entity.ToDto();
        dto.Categories = await GetCategoryDtosAsync(id, cancellationToken);
        return Result<BlogDto>.Ok(dto);
    }

    public async Task<Result<IEnumerable<BlogDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Blogs.GetAllAsync(cancellationToken);
        var dtos = entities.Select(e => e.ToDto()).ToList();
        await AttachCategoriesAsync(dtos, cancellationToken);
        return Result<IEnumerable<BlogDto>>.Ok(dtos);
    }

    public async Task<Result<IEnumerable<AdminBlogDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Blogs.GetAllForAdminAsync(cancellationToken);
        var dtos = entities.Select(e => e.ToAdminDto()).ToList();

        var map = await LoadCategoryMapAsync(dtos.Select(d => d.Id).ToList(), cancellationToken);
        foreach (var dto in dtos)
            if (map.TryGetValue(dto.Id, out var cats))
                dto.Categories = cats;

        return Result<IEnumerable<AdminBlogDto>>.Ok(dtos);
    }

    public async Task<Result<AdminBlogDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        var dto = entity.ToAdminDto();
        dto.Categories = await GetCategoryDtosAsync(id, cancellationToken);
        return Result<AdminBlogDto>.Ok(dto);
    }

    public async Task<Result<AdminBlogDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminBlogDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;

        await _unitOfWork.Blogs.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Blog aktiflik durumu değiştirildi. Id: {id}, Yeni durum: {entity.IsActive}", cancellationToken);

        return Result<AdminBlogDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<AdminBlogDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminBlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminBlogDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Blogs.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Blog geri yüklendi. Id: {id}", cancellationToken);

        return Result<AdminBlogDto>.Ok(entity.ToAdminDto());
    }

    public Task<PagedResult<BlogDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => GetPublishedPagedAsync(pageNumber, pageSize, categoryId: null, search: null, cancellationToken);

    public async Task<PagedResult<BlogDto>> GetPublishedPagedAsync(int pageNumber, int pageSize, int? categoryId, string? search, CancellationToken cancellationToken = default)
    {
        var (entities, total) = await _unitOfWork.Blogs.GetPublishedPageAsync(pageNumber, pageSize, categoryId, search, cancellationToken);
        var dtos = entities.Select(e => e.ToDto()).ToList();
        await AttachCategoriesAsync(dtos, cancellationToken);
        return PagedResult<BlogDto>.Ok(dtos, total, pageNumber, pageSize);
    }

    public async Task<Result<IEnumerable<BlogSitemapDto>>> GetSitemapAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _unitOfWork.Blogs.GetSitemapDataAsync(cancellationToken);
        var dtos = rows.Select(r => new BlogSitemapDto
        {
            Id = r.Id,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
        return Result<IEnumerable<BlogSitemapDto>>.Ok(dtos);
    }

    public async Task<Result<BlogDto>> CreateAsync(CreateBlogDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<BlogDto>.Fail("Başlık boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Content))
            return Result<BlogDto>.Fail("İçerik boş olamaz.");

        var entity = dto.ToEntity();
        await _unitOfWork.Blogs.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (dto.CategoryIds is { Count: > 0 })
        {
            await _unitOfWork.Blogs.SetCategoriesAsync(entity.Id, dto.CategoryIds, dto.CreatedBy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _activityLogger.LogAsync($"Blog oluşturuldu. Id: {entity.Id}", cancellationToken);

        var result = entity.ToDto();
        result.Categories = await GetCategoryDtosAsync(entity.Id, cancellationToken);
        return Result<BlogDto>.Ok(result);
    }

    public async Task<Result<BlogDto>> UpdateAsync(UpdateBlogDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
            return Result<BlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<BlogDto>.Fail("Başlık boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.Content))
            return Result<BlogDto>.Fail("İçerik boş olamaz.");

        entity.UpdateEntity(dto);
        await _unitOfWork.Blogs.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (dto.CategoryIds is not null)
        {
            await _unitOfWork.Blogs.SetCategoriesAsync(entity.Id, dto.CategoryIds, dto.UpdatedBy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _activityLogger.LogAsync($"Blog güncellendi. Id: {entity.Id}", cancellationToken);

        var result = entity.ToDto();
        result.Categories = await GetCategoryDtosAsync(entity.Id, cancellationToken);
        return Result<BlogDto>.Ok(result);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Blog bulunamadı.", statusCode: 404);

        await _unitOfWork.Blogs.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Blog silindi. Id: {id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Blogs.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private async Task<List<CategoryDto>> GetCategoryDtosAsync(int blogId, CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Blogs.GetCategoriesByBlogAsync(blogId, cancellationToken);
        return categories.Select(c => c.ToDto()).ToList();
    }

    private async Task<Dictionary<int, List<CategoryDto>>> LoadCategoryMapAsync(IReadOnlyCollection<int> blogIds, CancellationToken cancellationToken)
    {
        var raw = await _unitOfWork.Blogs.GetCategoriesForBlogsAsync(blogIds, cancellationToken);
        return raw.ToDictionary(kv => kv.Key, kv => kv.Value.Select(c => c.ToDto()).ToList());
    }

    private async Task AttachCategoriesAsync(IReadOnlyList<BlogDto> dtos, CancellationToken cancellationToken)
    {
        if (dtos.Count == 0) return;
        var map = await LoadCategoryMapAsync(dtos.Select(d => d.Id).ToList(), cancellationToken);
        foreach (var dto in dtos)
            if (map.TryGetValue(dto.Id, out var cats))
                dto.Categories = cats;
    }
}