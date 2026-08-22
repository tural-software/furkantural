using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Contact;

namespace FurkanTural_Business.Mappers;

public static class ContactMapper
{
    public static ContactDto ToDto(this Contact entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Email = entity.Email,
        Message = entity.Message,
        IsRead = entity.IsRead,
        CreatedAt = entity.CreatedAt
    };

    public static AdminContactDto ToAdminDto(this Contact entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Email = entity.Email,
        Message = entity.Message,
        IsRead = entity.IsRead,
        IpAddress = entity.IpAddress,
        UserAgent = entity.UserAgent,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static Contact ToEntity(this CreateContactDto dto) => new()
    {
        Name = dto.Name,
        Email = dto.Email,
        Message = dto.Message,
        IpAddress = dto.IpAddress,
        UserAgent = dto.UserAgent
    };
}