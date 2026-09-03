using FurkanTural_Application.DTOs.ProjectImage;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class ProjectImageMapper
{
    public static ProjectImageDto ToDto(this ProjectImage entity) => new()
    {
        Id = entity.Id,
        Url = entity.Url,
        AltText = entity.AltText,
        IsCover = entity.IsCover,
        ProjectId = entity.ProjectId
    };

    public static AdminProjectImageDto ToAdminDto(this ProjectImage entity) => new()
    {
        Id = entity.Id,
        Url = entity.Url,
        AltText = entity.AltText,
        IsCover = entity.IsCover,
        ProjectId = entity.ProjectId,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static ProjectImage ToEntity(this CreateProjectImageDto dto) => new()
    {
        Url = dto.Url,
        AltText = dto.AltText,
        IsCover = dto.IsCover,
        ProjectId = dto.ProjectId,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this ProjectImage entity, UpdateProjectImageDto dto)
    {
        entity.Url = dto.Url;
        entity.AltText = dto.AltText;
        entity.IsCover = dto.IsCover;
        entity.ProjectId = dto.ProjectId;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}