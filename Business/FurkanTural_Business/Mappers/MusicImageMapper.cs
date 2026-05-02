using FurkanTural_Application.DTOs.MusicImage;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class MusicImageMapper
{
    public static MusicImageDto ToDto(this MusicImage entity) => new()
    {
        Id = entity.Id,
        Url = entity.Url,
        MusicId = entity.MusicId
    };

    public static MusicImage ToEntity(this CreateMusicImageDto dto) => new()
    {
        Url = dto.Url,
        MusicId = dto.MusicId
    };

    public static void UpdateEntity(this MusicImage entity, UpdateMusicImageDto dto)
    {
        entity.Url = dto.Url;
        entity.MusicId = dto.MusicId;
    }
}