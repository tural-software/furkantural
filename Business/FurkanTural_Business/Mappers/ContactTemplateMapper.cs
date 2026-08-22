using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.ContactTemplate;

namespace FurkanTural_Business.Mappers;

public static class ContactTemplateMapper
{
    public static ContactTemplateDto ToDto(this ContactTemplate entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        TemplateType = entity.TemplateType,
        FileName = entity.FileName
    };

    public static AdminContactTemplateDto ToAdminDto(this ContactTemplate entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        TemplateType = entity.TemplateType,
        FileName = entity.FileName,
        HtmlContent = entity.HtmlContent,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static ContactTemplate ToEntity(this CreateContactTemplateDto dto) => new()
    {
        Name = dto.Name,
        TemplateType = dto.TemplateType,
        FileName = dto.FileName,
        HtmlContent = dto.HtmlContent,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this ContactTemplate entity, UpdateContactTemplateDto dto)
    {
        entity.Name = dto.Name;
        entity.TemplateType = dto.TemplateType;
        entity.FileName = dto.FileName;
        entity.HtmlContent = dto.HtmlContent;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}