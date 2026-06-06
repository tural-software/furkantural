using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.UserFriend;

namespace FurkanTural_Business.Mappers;

public static class UserFriendMapper
{
    public static UserFriendDto ToDto(this UserFriend entity) => new()
    {
        Id = entity.Id,
        RequesterId = entity.RequesterId,
        AddresseeId = entity.AddresseeId,
        StatusId = entity.StatusId,
        RespondedAt = entity.RespondedAt,
        CreatedAt = entity.CreatedAt
    };

    // StatusCode / StatusName servis tarafından statü sözlüğünden doldurulur.
    public static AdminUserFriendDto ToAdminDto(this UserFriend entity) => new()
    {
        Id = entity.Id,
        RequesterId = entity.RequesterId,
        AddresseeId = entity.AddresseeId,
        StatusId = entity.StatusId,
        RespondedAt = entity.RespondedAt,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };
}
