using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Music;

namespace FurkanTural_Business.Mappers;

public static class MusicMapper
{
    public static MusicDto ToDto(this Music entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Artist = entity.Artist,
        Productor = entity.Productor,
        Album = entity.Album,
        Genre = entity.Genre,
        Lyrics = entity.Lyrics,
        Duration = entity.Duration,
        ReleaseDate = entity.ReleaseDate,
        YouTubeMusicUrl = entity.YouTubeMusicUrl
    };

    public static AdminMusicDto ToAdminDto(this Music entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Artist = entity.Artist,
        Productor = entity.Productor,
        Album = entity.Album,
        Genre = entity.Genre,
        Lyrics = entity.Lyrics,
        Duration = entity.Duration,
        ReleaseDate = entity.ReleaseDate,
        YouTubeMusicUrl = entity.YouTubeMusicUrl,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static Music ToEntity(this CreateMusicDto dto) => new()
    {
        Name = dto.Name,
        Artist = dto.Artist,
        Productor = dto.Productor,
        Album = dto.Album,
        Genre = dto.Genre,
        Lyrics = dto.Lyrics,
        Duration = dto.Duration,
        ReleaseDate = dto.ReleaseDate,
        YouTubeMusicUrl = dto.YouTubeMusicUrl,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this Music entity, UpdateMusicDto dto)
    {
        entity.Name = dto.Name;
        entity.Artist = dto.Artist;
        entity.Productor = dto.Productor;
        entity.Album = dto.Album;
        entity.Genre = dto.Genre;
        entity.Lyrics = dto.Lyrics;
        entity.Duration = dto.Duration;
        entity.ReleaseDate = dto.ReleaseDate;
        entity.YouTubeMusicUrl = dto.YouTubeMusicUrl;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}