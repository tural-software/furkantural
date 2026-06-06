using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.ContactTemplate;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IContactTemplateService : IService<ContactTemplateDto, CreateContactTemplateDto, UpdateContactTemplateDto>
{
    Task<Result<IEnumerable<AdminContactTemplateDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminContactTemplateDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminContactTemplateDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminContactTemplateDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
