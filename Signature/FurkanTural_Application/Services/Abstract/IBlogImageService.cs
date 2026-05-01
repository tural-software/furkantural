using FurkanTural_Application.DTOs.BlogImage;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IBlogImageService : IService<BlogImageDto, CreateBlogImageDto, UpdateBlogImageDto>
{
    Task<Result<IEnumerable<BlogImageDto>>> GetByBlogIdAsync(int blogId, CancellationToken cancellationToken = default);
}