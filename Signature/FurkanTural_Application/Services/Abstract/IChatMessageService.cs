using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Sohbet mesajlarının tüm yaşam döngüsü. İçerik veri tabanında şifreli durur (<see cref="IMessageProtector"/>); şifreleme ve çözme bu servisin içinde yapılır, mapper'lar ham değeri taşır. GetConversationAsync arkadaş olmayan çiftte 403 döner; take verilirse en yeni N mesaj alınır. DeleteOwnAsync ve EditOwnAsync yalnızca gönderene açıktır, düzenleme ayrıca sadece metin mesajlarında ve gönderimden sonraki dar bir pencere içinde geçerlidir. ValidateAttachmentAccessAsync dosya adından yola çıkıp çağıranın o mesajın tarafı olduğunu doğrular; ek dosyaları korumasız statik yolla servis edilmesin diye vardır. EncryptLegacyContentAsync tek seferlik bir taşıma işidir: şifresiz kalmış eski kayıtları gruplar hâlinde şifreleyip kaç kaydın taşındığını döndürür.</summary>
public interface IChatMessageService
{
    Task<Result<ChatMessageDto>> SendAsync(int senderId, int receiverId, string? content, CancellationToken cancellationToken = default);
    Task<Result<ChatMessageDto>> SendAudioAsync(int senderId, int receiverId, string? fileName, int? durationSeconds, CancellationToken cancellationToken = default);
    Task<Result<ChatMessageDto>> SendMediaAsync(int senderId, int receiverId, string? fileName, string messageType, int? durationSeconds, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ChatMessageDto>>> GetConversationAsync(int currentUserId, int otherUserId, int? take = null, CancellationToken cancellationToken = default);
    Task<Result> MarkConversationReadAsync(int currentUserId, int otherUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ConversationSummaryDto>>> GetConversationsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<Result> ValidateAttachmentAccessAsync(int userId, string file, CancellationToken cancellationToken = default);
    Task<Result<ChatMessageDto>> DeleteOwnAsync(int userId, int messageId, CancellationToken cancellationToken = default);
    Task<Result<ChatMessageDto>> EditOwnAsync(int userId, int messageId, string? newContent, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminChatMessageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminChatMessageDto>> GetAllPagedForAdminAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminChatMessageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> EncryptLegacyContentAsync(CancellationToken cancellationToken = default);
}
