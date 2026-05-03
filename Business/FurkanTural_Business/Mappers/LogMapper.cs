using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Log;

namespace FurkanTural_Business.Mappers;

public static class LogMapper
{
    public static LogDto ToDto(this Log entity) => new()
    {
        Id = entity.Id,
        Project = entity.Project,
        Date = entity.Date,
        Level = entity.Level,
        Message = entity.Message,
        Detail = entity.Detail,
        IpAddress = entity.IpAddress,
        Path = entity.Path
    };

    public static Log ToEntity(this CreateLogDto dto) => new()
    {
        Project = dto.Project,
        Date = dto.Date,
        Level = dto.Level,
        Message = dto.Message,
        Detail = dto.Detail,
        IpAddress = dto.IpAddress,
        Path = dto.Path
    };
}