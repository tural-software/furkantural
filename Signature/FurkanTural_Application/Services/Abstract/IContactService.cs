using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Contact;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IContactService
{
    Task<Result> SubmitAsync(SubmitContactDto dto, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<Result<ContactDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ContactDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ContactDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminContactDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminContactDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminContactDto>> MarkAsReadAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminContactDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminContactDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
