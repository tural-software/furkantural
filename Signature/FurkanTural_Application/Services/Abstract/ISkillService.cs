using FurkanTural_Application.DTOs.Skill;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface ISkillService : IService<SkillDto, CreateSkillDto, UpdateSkillDto>
{
    Task<Result<AdminSkillDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
}