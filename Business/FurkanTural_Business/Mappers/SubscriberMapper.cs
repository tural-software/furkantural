using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Subscriber;

namespace FurkanTural_Business.Mappers;

public static class SubscriberMapper
{
    public static SubscriberDto ToDto(this Subscriber entity) => new()
    {
        Id = entity.Id,
        Email = entity.Email
    };

    public static AdminSubscriberDto ToAdminDto(this Subscriber entity) => new()
    {
        Id = entity.Id,
        Email = entity.Email,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static Subscriber ToEntity(this CreateSubscriberDto dto) => new()
    {
        Email = dto.Email
    };

    public static void UpdateEntity(this Subscriber entity, UpdateSubscriberDto dto)
    {
        entity.Email = dto.Email;
    }
}