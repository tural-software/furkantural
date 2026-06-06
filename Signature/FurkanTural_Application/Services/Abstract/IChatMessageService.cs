using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IChatMessageService
{
    // ── Üye işlemleri ──
    Task<Result<ChatMessageDto>> SendAsync(int senderId, int receiverId, string? content, CancellationToken cancellationToken = default);
    Task<Result<ChatMessageDto>> SendAudioAsync(int senderId, int receiverId, string? fileName, int? durationSeconds, CancellationToken cancellationToken = default);
    /// <summary>Foto/Video mesajı kalıcılaştırır. <paramref name="messageType"/> = "Image" | "Video".</summary>
    Task<Result<ChatMessageDto>> SendMediaAsync(int senderId, int receiverId, string? fileName, string messageType, int? durationSeconds, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ChatMessageDto>>> GetConversationAsync(int currentUserId, int otherUserId, int? take = null, CancellationToken cancellationToken = default);
    Task<Result> MarkConversationReadAsync(int currentUserId, int otherUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ConversationSummaryDto>>> GetConversationsAsync(int currentUserId, CancellationToken cancellationToken = default);

    // ── Admin ──
    Task<Result<IEnumerable<AdminChatMessageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminChatMessageDto>> GetAllPagedForAdminAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
