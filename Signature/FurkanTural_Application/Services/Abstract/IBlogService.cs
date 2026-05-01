using FurkanTural_Application.DTOs.Blog;

namespace FurkanTural_Application.Services.Abstract;

public interface IBlogService : IService<BlogDto, CreateBlogDto, UpdateBlogDto> {  }