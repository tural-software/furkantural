using FurkanTural_Application.DTOs.Experience;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IExperienceService : IService<ExperienceDto, CreateExperienceDto, UpdateExperienceDto>
{
    Task<Result<IEnumerable<AdminExperienceDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminExperienceDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminExperienceDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminExperienceDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
}
