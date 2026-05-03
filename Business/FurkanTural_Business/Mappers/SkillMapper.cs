using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Skill;

namespace FurkanTural_Business.Mappers;

public static class SkillMapper
{
    public static SkillDto ToDto(this Skill entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Proficiency = entity.Proficiency
    };

    public static Skill ToEntity(this CreateSkillDto dto) => new()
    {
        Name = dto.Name,
        Proficiency = dto.Proficiency
    };

    public static void UpdateEntity(this Skill entity, UpdateSkillDto dto)
    {
        entity.Name = dto.Name;
        entity.Proficiency = dto.Proficiency;
    }
}