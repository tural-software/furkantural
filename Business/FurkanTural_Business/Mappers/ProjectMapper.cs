using FurkanTural_Application.DTOs.Project;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class ProjectMapper
{
    public static ProjectDto ToDto(this Project entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        ShortDescription = entity.ShortDescription,
        TechStack = entity.TechStack,
        GitHubUrl = entity.GitHubUrl,
        DemoUrl = entity.DemoUrl,
        IsCompleted = entity.IsCompleted
    };

    public static AdminProjectDto ToAdminDto(this Project entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        ShortDescription = entity.ShortDescription,
        TechStack = entity.TechStack,
        GitHubUrl = entity.GitHubUrl,
        DemoUrl = entity.DemoUrl,
        IsCompleted = entity.IsCompleted,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static Project ToEntity(this CreateProjectDto dto) => new()
    {
        Title = dto.Title,
        Description = dto.Description,
        ShortDescription = dto.ShortDescription,
        TechStack = dto.TechStack,
        GitHubUrl = dto.GitHubUrl,
        DemoUrl = dto.DemoUrl,
        IsCompleted = dto.IsCompleted,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this Project entity, UpdateProjectDto dto)
    {
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.ShortDescription = dto.ShortDescription;
        entity.TechStack = dto.TechStack;
        entity.GitHubUrl = dto.GitHubUrl;
        entity.DemoUrl = dto.DemoUrl;
        entity.IsCompleted = dto.IsCompleted;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}