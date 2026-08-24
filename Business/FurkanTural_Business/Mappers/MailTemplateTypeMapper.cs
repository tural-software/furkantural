using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.DTOs.MailTemplateType;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class MailTemplateTypeMapper
{
    public static MailTemplateTypeDto ToDto(this MailTemplateType entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        SortOrder = entity.SortOrder,
        Placeholders = MailPayloads.PlaceholdersOf(entity.Code)
    };

    public static AdminMailTemplateTypeDto ToAdminDto(this MailTemplateType entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        SortOrder = entity.SortOrder,
        Placeholders = MailPayloads.PlaceholdersOf(entity.Code),
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static MailTemplateType ToEntity(this CreateMailTemplateTypeDto dto) => new()
    {
        Code = dto.Code,
        Name = dto.Name,
        Description = dto.Description,
        SortOrder = dto.SortOrder,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this MailTemplateType entity, UpdateMailTemplateTypeDto dto)
    {
        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.SortOrder = dto.SortOrder;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}
