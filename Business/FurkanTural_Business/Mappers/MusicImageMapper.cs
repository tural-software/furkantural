using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.MusicImage;

namespace FurkanTural_Business.Mappers;

public static class MusicImageMapper
{
    public static MusicImageDto ToDto(this MusicImage entity) => new()
    {
        Id = entity.Id,
        Url = entity.Url,
        MusicId = entity.MusicId
    };

    public static AdminMusicImageDto ToAdminDto(this MusicImage entity) => new()
    {
        Id = entity.Id,
        Url = entity.Url,
        MusicId = entity.MusicId,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static MusicImage ToEntity(this CreateMusicImageDto dto) => new()
    {
        Url = dto.Url,
        MusicId = dto.MusicId,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this MusicImage entity, UpdateMusicImageDto dto)
    {
        entity.Url = dto.Url;
        entity.MusicId = dto.MusicId;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}