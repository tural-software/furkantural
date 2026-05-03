using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IBlogService : IService<BlogDto, CreateBlogDto, UpdateBlogDto>
{
    Task<Result<IEnumerable<BlogDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
}