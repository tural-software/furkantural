using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;

namespace FurkanTural_Business.Services.Concrete;

public class BlogService(IUnitOfWork unitOfWork) : IBlogService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<BlogDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<BlogDto>.Fail("Blog bulunamadı.", statusCode: 404);

        return Result<BlogDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<BlogDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Blogs.GetAllAsync(cancellationToken);
        return Result<IEnumerable<BlogDto>>.Ok(entities.Select(e => e.ToDto()));
    }

    public async Task<Result<IEnumerable<AdminBlogDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Blogs.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminBlogDto>>.Ok(entities.Select(e => e.ToAdminDto()));
    }

    public async Task<PagedResult<BlogDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.Blogs.GetAllPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var total = await _unitOfWork.Blogs.CountAsync(cancellationToken: cancellationToken);
        return PagedResult<BlogDto>.Ok(entities.Select(e => e.ToDto()), total, pageNumber, pageSize);
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

        return Result<BlogDto>.Ok(entity.ToDto());
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

        return Result<BlogDto>.Ok(entity.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Blogs.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result.Fail("Blog bulunamadı.", statusCode: 404);

        await _unitOfWork.Blogs.SoftDeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}