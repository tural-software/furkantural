using FurkanTural_Application.DTOs.Subscriber;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class SubscriberMapper
{
    public static SubscriberDto ToDto(this Subscriber entity) => new()
    {
        Id = entity.Id,
        Email = entity.Email
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