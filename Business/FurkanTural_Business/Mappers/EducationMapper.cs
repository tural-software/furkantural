using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Education;

namespace FurkanTural_Business.Mappers;

public static class EducationMapper
{
    public static EducationDto ToDto(this Education entity) => new()
    {
        Id = entity.Id,
        Institution = entity.Institution,
        Degree = entity.Degree,
        FieldOfStudy = entity.FieldOfStudy,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate
    };

    public static Education ToEntity(this CreateEducationDto dto) => new()
    {
        Institution = dto.Institution,
        Degree = dto.Degree,
        FieldOfStudy = dto.FieldOfStudy,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this Education entity, UpdateEducationDto dto)
    {
        entity.Institution = dto.Institution;
        entity.Degree = dto.Degree;
        entity.FieldOfStudy = dto.FieldOfStudy;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}