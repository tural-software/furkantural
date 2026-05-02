using FurkanTural_Application.DTOs.Blog;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class BlogMapper
{
    public static BlogDto ToDto(this Blog entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Content = entity.Content
    };

    public static Blog ToEntity(this CreateBlogDto dto) => new()
    {
        Title = dto.Title,
        Content = dto.Content
    };

    public static void UpdateEntity(this Blog entity, UpdateBlogDto dto)
    {
        entity.Title = dto.Title;
        entity.Content = dto.Content;
    }
}