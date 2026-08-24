using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.MailTemplate;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Posta şablonlarının yönetimi. Tür başına yalnızca bir şablon etkin olabilir; ikinci bir şablonu etkinleştirme denemesi veri tabanı kısıtına takılır ve çakışma yanıtına dönüşür. Taslak tutmak için sayı sınırı yoktur, çünkü kısıt yalnızca etkin satırları kapsar.</summary>
public interface IMailTemplateService : IService<MailTemplateDto, CreateMailTemplateDto, UpdateMailTemplateDto>
{
    Task<Result<IEnumerable<AdminMailTemplateDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminMailTemplateDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminMailTemplateDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminMailTemplateDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
