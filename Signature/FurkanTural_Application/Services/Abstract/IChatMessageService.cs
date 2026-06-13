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

    /// <summary>Ekin (ses/foto/video) kullanıcının taraf olduğu bir mesaja ait olduğunu doğrular (chat ekleri statik sunulmaz).</summary>
    Task<Result> ValidateAttachmentAccessAsync(int userId, string file, CancellationToken cancellationToken = default);

    /// <summary>Gönderenin kendi mesajını silmesi (soft delete; her iki taraftan kalkar, admin geri yükleyebilir).</summary>
    Task<Result<ChatMessageDto>> DeleteOwnAsync(int userId, int messageId, CancellationToken cancellationToken = default);

    /// <summary>Gönderenin kendi Text mesajını düzenlemesi (gönderimden sonraki 15 dakika içinde).</summary>
    Task<Result<ChatMessageDto>> EditOwnAsync(int userId, int messageId, string? newContent, CancellationToken cancellationToken = default);

    // ── Admin ──
    Task<Result<IEnumerable<AdminChatMessageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminChatMessageDto>> GetAllPagedForAdminAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// At-rest şifreleme öncesinden kalan düz metin mesajları toplu olarak şifreler (idempotent, tek seferlik).
    /// Şifrelenen kayıt sayısını döner. Operatör tetikler; başlangıçta otomatik çalışmaz.
    /// </summary>
    Task<Result<int>> EncryptLegacyContentAsync(CancellationToken cancellationToken = default);
}
