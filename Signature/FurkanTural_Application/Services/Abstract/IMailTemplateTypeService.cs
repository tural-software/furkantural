using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.MailTemplateType;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Posta türlerinin yönetimi. Tohumla gelen türler kod tarafındaki gönderim yollarına bağlıdır; kodlarının değiştirilmesi o yolları sessizce şablonsuz bırakır, çünkü eşleşme Id ile değil Code ile kurulur.<para>Panelden eklenen tür şablon taşıyabilir ama kendiliğinden postaya dönüşmez: onu gönderen bir çağıran ve alanlarını tanımlayan bir gövde DTO'su yoktur.</para></summary>
public interface IMailTemplateTypeService : IService<MailTemplateTypeDto, CreateMailTemplateTypeDto, UpdateMailTemplateTypeDto>
{
    Task<Result<IEnumerable<AdminMailTemplateTypeDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminMailTemplateTypeDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminMailTemplateTypeDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminMailTemplateTypeDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
