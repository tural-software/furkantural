using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.UserFriend;

namespace FurkanTural_Business.Mappers;

/// <summary>ToAdminDto yalnızca StatusId'yi taşır; StatusCode ve StatusName boş kalır. Bunlar statü sözlüğünden okunduğu için servis katmanında doldurulur.</summary>
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
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };
}
