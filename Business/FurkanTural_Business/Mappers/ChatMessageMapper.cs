using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.ChatMessage;

namespace FurkanTural_Business.Mappers;

public static class ChatMessageMapper
{
    public static ChatMessageDto ToDto(this ChatMessage entity) => new()
    {
        Id = entity.Id,
        SenderId = entity.SenderId,
        ReceiverId = entity.ReceiverId,
        Content = entity.Content,
        CreatedAt = entity.CreatedAt,
        IsRead = entity.IsRead,
        ReadAt = entity.ReadAt,
        MessageType = entity.MessageType,
        AttachmentUrl = entity.AttachmentUrl,
        DurationSeconds = entity.DurationSeconds,
        EditedAt = entity.EditedAt
    };

    public static AdminChatMessageDto ToAdminDto(this ChatMessage entity) => new()
    {
        Id = entity.Id,
        SenderId = entity.SenderId,
        ReceiverId = entity.ReceiverId,
        Content = entity.Content,
        IsRead = entity.IsRead,
        ReadAt = entity.ReadAt,
        MessageType = entity.MessageType,
        AttachmentUrl = entity.AttachmentUrl,
        DurationSeconds = entity.DurationSeconds,
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