using FurkanTural_Application.DTOs.Mail;
using FurkanTural_Application.DTOs.MailTemplate;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

/// <summary>Tür ve proje bilgisi ayrı parametrelerle gelir çünkü entity'ler gezinme özelliği taşımaz; birleştirmeyi servis katmanı yapar. Tür verilmezse kod ve ad boş kalır, yer tutucu listesi de boş döner. Proje boş olmak zaten geçerli bir durumdur: şablon tüm projeler için geçerli genel sürümdür.</summary>
public static class MailTemplateMapper
{
    public static MailTemplateDto ToDto(this MailTemplate entity, MailTemplateType? type, AppSource? appSource) => new()
    {
        Id = entity.Id,
        MailTemplateTypeId = entity.MailTemplateTypeId,
        TypeCode = type?.Code,
        TypeName = type?.Name,
        AppSourceId = entity.AppSourceId,
        AppSourceName = appSource?.Name,
        Name = entity.Name,
        Subject = entity.Subject,
        FileName = entity.FileName
    };

    public static AdminMailTemplateDto ToAdminDto(this MailTemplate entity, MailTemplateType? type, AppSource? appSource) => new()
    {
        Id = entity.Id,
        MailTemplateTypeId = entity.MailTemplateTypeId,
        TypeCode = type?.Code,
        TypeName = type?.Name,
        AppSourceId = entity.AppSourceId,
        AppSourceCode = appSource?.Code,
        AppSourceName = appSource?.Name,
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
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static MailTemplate ToEntity(this CreateMailTemplateDto dto) => new()
    {
        MailTemplateTypeId = dto.MailTemplateTypeId,
        AppSourceId = dto.AppSourceId,
        Name = dto.Name,
        Subject = dto.Subject,
        HtmlContent = dto.HtmlContent,
        FileName = dto.FileName,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this MailTemplate entity, UpdateMailTemplateDto dto)
    {
        entity.MailTemplateTypeId = dto.MailTemplateTypeId;
        entity.AppSourceId = dto.AppSourceId;
        entity.Name = dto.Name;
        entity.Subject = dto.Subject;
        entity.HtmlContent = dto.HtmlContent;
        entity.FileName = dto.FileName;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}
