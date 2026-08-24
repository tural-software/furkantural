using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.DTOs.MailTemplate;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

/// <summary>Tür bilgisi ayrı parametreyle gelir çünkü entity'ler gezinme özelliği taşımaz; birleştirmeyi servis katmanı yapar. Tür verilmezse kod ve ad boş kalır, yer tutucu listesi de boş döner.</summary>
public static class MailTemplateMapper
{
    public static MailTemplateDto ToDto(this MailTemplate entity, MailTemplateType? type) => new()
    {
        Id = entity.Id,
        MailTemplateTypeId = entity.MailTemplateTypeId,
        TypeCode = type?.Code,
        TypeName = type?.Name,
        Name = entity.Name,
        Subject = entity.Subject,
        FileName = entity.FileName
    };

    public static AdminMailTemplateDto ToAdminDto(this MailTemplate entity, MailTemplateType? type) => new()
    {
        Id = entity.Id,
        MailTemplateTypeId = entity.MailTemplateTypeId,
        TypeCode = type?.Code,
        TypeName = type?.Name,
        Name = entity.Name,
        Subject = entity.Subject,
        HtmlContent = entity.HtmlContent,
        FileName = entity.FileName,
        Placeholders = MailPayloads.PlaceholdersOf(type?.Code),
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static MailTemplate ToEntity(this CreateMailTemplateDto dto) => new()
    {
        MailTemplateTypeId = dto.MailTemplateTypeId,
        Name = dto.Name,
        Subject = dto.Subject,
        HtmlContent = dto.HtmlContent,
        FileName = dto.FileName,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this MailTemplate entity, UpdateMailTemplateDto dto)
    {
        entity.MailTemplateTypeId = dto.MailTemplateTypeId;
        entity.Name = dto.Name;
        entity.Subject = dto.Subject;
        entity.HtmlContent = dto.HtmlContent;
        entity.FileName = dto.FileName;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}
