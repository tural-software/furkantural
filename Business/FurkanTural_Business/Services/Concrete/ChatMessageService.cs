using System.Linq.Expressions;
using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Yeni gönderilen mesajın düz metni çağırana elde olan değerden verilir, kaydedilen şifreli değer tekrar çözülmez. Yönetim görünümleri de içeriği çözülmüş alır — panel araması düz metin üzerinde çalışsın diye — dolayısıyla mesaj içeriği yönetim uçlarından da okunabilir.<para>Kullanıcı adları yönetim tarafında süzgeçsiz okumayla çözülür: silinmiş kullanıcı normal okumada görünmez ve tam da yöneticinin görmesi gereken satırlarda ad boş kalırdı.</para><para>GetConversationsAsync arkadaş başına sorgu açmaz; konuşma sayaçları tek toplulaştırma sorgusundan gelir.</para></summary>
public class ChatMessageService(
    IUnitOfWork unitOfWork,
    IUserFriendService userFriendService,
    IMessageRateLimiter messageRateLimiter,
    IMessageProtector messageProtector,
    IPresenceTracker presenceTracker,
    IPushSender pushSender,
    ActivityLogger activityLogger,
    IClock clock) : IChatMessageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserFriendService _userFriendService = userFriendService;
    private readonly IMessageRateLimiter _messageRateLimiter = messageRateLimiter;
    private readonly IMessageProtector _messageProtector = messageProtector;
    private readonly IPresenceTracker _presenceTracker = presenceTracker;
    private readonly IPushSender _pushSender = pushSender;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly IClock _clock = clock;

    private async Task PushIfReceiverOfflineAsync(int senderId, int receiverId, CancellationToken cancellationToken)
    {
        if (_presenceTracker.IsOnline(receiverId))
            return;

        var sender = await _unitOfWork.Users.GetByIdAsync(senderId, cancellationToken);
        var name = sender?.DisplayName ?? sender?.Username ?? "Biri";
        await _pushSender.SendMessageNotificationAsync(receiverId, name, cancellationToken);
    }

    private const int MaxContentLength = 4000;

    private static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(15);

    public async Task<Result<ChatMessageDto>> SendAsync(int senderId, int receiverId, string? content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result<ChatMessageDto>.Fail("Mesaj boş olamaz.");

        if (content.Trim().Length > MaxContentLength)
            return Result<ChatMessageDto>.Fail($"Mesaj en fazla {MaxContentLength} karakter olabilir.");

        if (senderId == receiverId)
            return Result<ChatMessageDto>.Fail("Kendinize mesaj gönderemezsiniz.");

        if (!_messageRateLimiter.TryRegisterSend(senderId))
            return Result<ChatMessageDto>.Fail("Çok hızlı mesaj gönderiyorsunuz. Lütfen kısa bir süre bekleyin.", statusCode: 429);

        var areFriends = await _userFriendService.AreFriendsAsync(senderId, receiverId, cancellationToken);
        if (!areFriends)
            return Result<ChatMessageDto>.Fail("Yalnızca arkadaşlarınıza mesaj gönderebilirsiniz.", statusCode: 403);

        if (await _userFriendService.IsBlockedBetweenAsync(senderId, receiverId, cancellationToken))
            return Result<ChatMessageDto>.Fail("Bu kullanıcıyla mesajlaşamazsınız.", statusCode: 403);

        var plaintext = content.Trim();
        var entity = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = _messageProtector.Protect(plaintext),
            MessageType = "Text",
            IsRead = false
        };
        await _unitOfWork.ChatMessages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PushIfReceiverOfflineAsync(senderId, receiverId, cancellationToken);

        var dto = entity.ToDto();
        dto.Content = plaintext;
        return Result<ChatMessageDto>.Ok(dto);
    }

    public async Task<Result<ChatMessageDto>> SendAudioAsync(int senderId, int receiverId, string? fileName, int? durationSeconds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result<ChatMessageDto>.Fail("Ses dosyası gerekli.");

        if (senderId == receiverId)
            return Result<ChatMessageDto>.Fail("Kendinize mesaj gönderemezsiniz.");

        if (!_messageRateLimiter.TryRegisterSend(senderId))
            return Result<ChatMessageDto>.Fail("Çok hızlı mesaj gönderiyorsunuz. Lütfen kısa bir süre bekleyin.", statusCode: 429);

        var areFriends = await _userFriendService.AreFriendsAsync(senderId, receiverId, cancellationToken);
        if (!areFriends)
            return Result<ChatMessageDto>.Fail("Yalnızca arkadaşlarınıza mesaj gönderebilirsiniz.", statusCode: 403);

        if (await _userFriendService.IsBlockedBetweenAsync(senderId, receiverId, cancellationToken))
            return Result<ChatMessageDto>.Fail("Bu kullanıcıyla mesajlaşamazsınız.", statusCode: 403);

        var entity = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageType = "Audio",
            AttachmentUrl = fileName,
            DurationSeconds = durationSeconds,
            IsRead = false
        };
        await _unitOfWork.ChatMessages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PushIfReceiverOfflineAsync(senderId, receiverId, cancellationToken);

        return Result<ChatMessageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<ChatMessageDto>> SendMediaAsync(int senderId, int receiverId, string? fileName, string messageType, int? durationSeconds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result<ChatMessageDto>.Fail("Medya dosyası gerekli.");

        if (!ChatMessageTypes.IsMedia(messageType))
            return Result<ChatMessageDto>.Fail("Geçersiz medya türü.");

        if (senderId == receiverId)
            return Result<ChatMessageDto>.Fail("Kendinize mesaj gönderemezsiniz.");

        if (!_messageRateLimiter.TryRegisterSend(senderId))
            return Result<ChatMessageDto>.Fail("Çok hızlı mesaj gönderiyorsunuz. Lütfen kısa bir süre bekleyin.", statusCode: 429);

        var areFriends = await _userFriendService.AreFriendsAsync(senderId, receiverId, cancellationToken);
        if (!areFriends)
            return Result<ChatMessageDto>.Fail("Yalnızca arkadaşlarınıza mesaj gönderebilirsiniz.", statusCode: 403);

        if (await _userFriendService.IsBlockedBetweenAsync(senderId, receiverId, cancellationToken))
            return Result<ChatMessageDto>.Fail("Bu kullanıcıyla mesajlaşamazsınız.", statusCode: 403);

        var entity = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageType = ChatMessageTypes.IsMedia(messageType) && string.Equals(messageType, ChatMessageTypes.Video, StringComparison.OrdinalIgnoreCase)
                ? ChatMessageTypes.Video : ChatMessageTypes.Image,
            AttachmentUrl = fileName,
            DurationSeconds = durationSeconds,
            IsRead = false
        };
        await _unitOfWork.ChatMessages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PushIfReceiverOfflineAsync(senderId, receiverId, cancellationToken);

        return Result<ChatMessageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<ChatMessageDto>>> GetConversationAsync(int currentUserId, int otherUserId, int? take = null, CancellationToken cancellationToken = default)
    {
        var areFriends = await _userFriendService.AreFriendsAsync(currentUserId, otherUserId, cancellationToken);
        if (!areFriends)
            return Result<IEnumerable<ChatMessageDto>>.Fail("Yalnızca arkadaşlarınızla olan sohbeti görüntüleyebilirsiniz.", statusCode: 403);

        var limit = take is > 0 ? Math.Min(take.Value, 500) : 100;
        var messages = await _unitOfWork.ChatMessages.GetAllPagedAsync(1, limit,
            x => (x.SenderId == currentUserId && x.ReceiverId == otherUserId) ||
                 (x.SenderId == otherUserId && x.ReceiverId == currentUserId),
            true,
            cancellationToken);

        return Result<IEnumerable<ChatMessageDto>>.Ok(messages.OrderBy(m => m.CreatedAt).Select(ToDecryptedDto));
    }

    private ChatMessageDto ToDecryptedDto(ChatMessage entity)
    {
        var dto = entity.ToDto();
        dto.Content = _messageProtector.Unprotect(dto.Content);
        return dto;
    }

    private AdminChatMessageDto ToDecryptedAdminDto(ChatMessage entity, IReadOnlyDictionary<int, string?> usernames)
    {
        var dto = entity.ToAdminDto();
        dto.Content = _messageProtector.Unprotect(dto.Content);
        dto.SenderUsername = usernames.GetValueOrDefault(entity.SenderId);
        dto.ReceiverUsername = usernames.GetValueOrDefault(entity.ReceiverId);
        return dto;
    }

    private async Task<Dictionary<int, string?>> LoadAllUsernamesAsync(CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllForAdminAsync(cancellationToken);
        return users.ToDictionary(u => u.Id, u => (string?)u.Username);
    }

    private async Task<Dictionary<int, string?>> LoadUsernamesForAsync(ChatMessage entity, CancellationToken cancellationToken)
    {
        var dict = new Dictionary<int, string?>();
        foreach (var id in new[] { entity.SenderId, entity.ReceiverId }.Distinct())
        {
            var user = await _unitOfWork.Users.GetByIdForAdminAsync(id, cancellationToken);
            if (user is not null) dict[id] = user.Username;
        }
        return dict;
    }

    public async Task<Result> MarkConversationReadAsync(int currentUserId, int otherUserId, CancellationToken cancellationToken = default)
    {
        var unread = (await _unitOfWork.ChatMessages.GetAllAsync(
            x => x.SenderId == otherUserId && x.ReceiverId == currentUserId && !x.IsRead,
            cancellationToken)).ToList();

        if (unread.Count == 0)
            return Result.Ok();

        var now = _clock.UtcNow;
        foreach (var message in unread)
        {
            message.IsRead = true;
            message.ReadAt = now;
        }

        await _unitOfWork.ChatMessages.UpdateRangeAsync(unread, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<ConversationSummaryDto>>> GetConversationsAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var friendsResult = await _userFriendService.GetFriendsAsync(currentUserId, cancellationToken);
        var friends = friendsResult.Data ?? [];

        var aggregates = (await _unitOfWork.ChatMessages.GetConversationAggregatesAsync(currentUserId, cancellationToken))
            .ToDictionary(a => a.OtherUserId);

        var summaries = friends.Select(friend =>
        {
            aggregates.TryGetValue(friend.FriendUserId, out var agg);
            return new ConversationSummaryDto
            {
                FriendUserId = friend.FriendUserId,
                Username = friend.Username,
                DisplayName = friend.DisplayName,
                AvatarUrl = friend.AvatarUrl,
                LastMessage = _messageProtector.Unprotect(agg?.LastMessage),
                LastMessageType = agg?.LastMessageType,
                LastMessageAt = agg?.LastMessageAt,
                UnreadCount = agg?.UnreadCount ?? 0
            };
        });

        return Result<IEnumerable<ConversationSummaryDto>>.Ok(summaries.OrderByDescending(s => s.LastMessageAt ?? DateTime.MinValue));
    }

    public async Task<Result> ValidateAttachmentAccessAsync(int userId, string file, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(file))
            return Result.Fail("Dosya belirtilmedi.", statusCode: 400);

        var allowed = await _unitOfWork.ChatMessages.AnyAsync(
            x => x.AttachmentUrl == file && (x.SenderId == userId || x.ReceiverId == userId),
            cancellationToken);

        return allowed
            ? Result.Ok()
            : Result.Fail("Bu dosyaya erişim yetkiniz yok.", statusCode: 403);
    }

    public async Task<Result<ChatMessageDto>> DeleteOwnAsync(int userId, int messageId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ChatMessages.GetAsync(x => x.Id == messageId, cancellationToken);
        if (entity is null)
            return Result<ChatMessageDto>.Fail("Mesaj bulunamadı.", statusCode: 404);

        if (entity.SenderId != userId)
            return Result<ChatMessageDto>.Fail("Yalnızca kendi mesajınızı silebilirsiniz.", statusCode: 403);

        await _unitOfWork.ChatMessages.SoftDeleteAsync(entity, userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChatMessageDto>.Ok(ToDecryptedDto(entity));
    }

    public async Task<Result<ChatMessageDto>> EditOwnAsync(int userId, int messageId, string? newContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newContent))
            return Result<ChatMessageDto>.Fail("Mesaj boş olamaz.");

        var trimmed = newContent.Trim();
        if (trimmed.Length > MaxContentLength)
            return Result<ChatMessageDto>.Fail($"Mesaj en fazla {MaxContentLength} karakter olabilir.");

        var entity = await _unitOfWork.ChatMessages.GetAsync(x => x.Id == messageId, cancellationToken);
        if (entity is null)
            return Result<ChatMessageDto>.Fail("Mesaj bulunamadı.", statusCode: 404);

        if (entity.SenderId != userId)
            return Result<ChatMessageDto>.Fail("Yalnızca kendi mesajınızı düzenleyebilirsiniz.", statusCode: 403);

        var isText = string.IsNullOrEmpty(entity.MessageType)
                  || string.Equals(entity.MessageType, ChatMessageTypes.Text, StringComparison.OrdinalIgnoreCase);
        if (!isText)
            return Result<ChatMessageDto>.Fail("Yalnızca metin mesajları düzenlenebilir.");

        if (_clock.UtcNow - entity.CreatedAt > EditWindow)
            return Result<ChatMessageDto>.Fail($"Mesaj yalnızca gönderildikten sonraki {(int)EditWindow.TotalMinutes} dakika içinde düzenlenebilir.");

        entity.Content = _messageProtector.Protect(trimmed);
        entity.EditedAt = _clock.UtcNow;
        await _unitOfWork.ChatMessages.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = entity.ToDto();
        dto.Content = trimmed;
        return Result<ChatMessageDto>.Ok(dto);
    }

    public async Task<Result<IEnumerable<AdminChatMessageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ChatMessages.GetAllForAdminAsync(cancellationToken);
        var usernames = await LoadAllUsernamesAsync(cancellationToken);
        return Result<IEnumerable<AdminChatMessageDto>>.Ok(entities.OrderByDescending(e => e.CreatedAt).Select(e => ToDecryptedAdminDto(e, usernames)));
    }

    public async Task<PagedResult<AdminChatMessageDto>> GetAllPagedForAdminAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => await GetAllForAdminPagedAsync(new AdminListQuery { PageNumber = pageNumber, PageSize = pageSize }, null, null, cancellationToken);

    public async Task<Result<AdminChatMessageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ChatMessages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminChatMessageDto>.Fail("Mesaj bulunamadı.", statusCode: 404);

        return Result<AdminChatMessageDto>.Ok(ToDecryptedAdminDto(entity, await LoadUsernamesForAsync(entity, cancellationToken)));
    }

    public async Task<Result<AdminChatMessageDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ChatMessages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminChatMessageDto>.Fail("Mesaj bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminChatMessageDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.ChatMessages.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminChatMessageDto>.Ok(ToDecryptedAdminDto(entity, await LoadUsernamesForAsync(entity, cancellationToken)));
    }

    public async Task<Result<AdminChatMessageDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ChatMessages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminChatMessageDto>.Fail("Mesaj bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminChatMessageDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.ChatMessages.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminChatMessageDto>.Ok(ToDecryptedAdminDto(entity, await LoadUsernamesForAsync(entity, cancellationToken)));
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.ChatMessages.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    public async Task<Result<int>> EncryptLegacyContentAsync(CancellationToken cancellationToken = default)
    {
        var all = await _unitOfWork.ChatMessages.GetAllForAdminAsync(cancellationToken);
        var pending = all.Where(m => !string.IsNullOrEmpty(m.Content) && !_messageProtector.IsProtected(m.Content)).ToList();
        if (pending.Count == 0)
            return Result<int>.Ok(0);

        var migrated = 0;
        foreach (var batch in pending.Chunk(500))
        {
            foreach (var entity in batch)
                entity.Content = _messageProtector.Protect(entity.Content);

            await _unitOfWork.ChatMessages.UpdateRangeAsync(batch, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            migrated += batch.Length;
        }

        await _activityLogger.LogAsync($"Legacy mesaj içerikleri şifrelendi. Adet: {migrated}", cancellationToken);
        return Result<int>.Ok(migrated);
    }

    private async Task<Expression<Func<ChatMessage, bool>>?> AdminPredicateAsync(AdminListQuery query, string? username, string? messageType, CancellationToken cancellationToken)
    {
        var predicate = AdminFilters.Common<ChatMessage>(query);
        if (!string.IsNullOrWhiteSpace(messageType))
        {
            var type = messageType.Trim();
            predicate = predicate.AndAlso(x => (x.MessageType ?? "Text") == type);
        }
        if (!string.IsNullOrWhiteSpace(username))
        {
            var name = username.Trim();
            var matched = (await _unitOfWork.Users.GetAllForAdminAsync(u => u.Username != null && u.Username.Contains(name), cancellationToken))
                .Select(u => u.Id).ToList();
            predicate = predicate.AndAlso(x => matched.Contains(x.SenderId) || matched.Contains(x.ReceiverId));
        }
        return predicate;
    }

    public async Task<PagedResult<AdminChatMessageDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? username, string? messageType, CancellationToken cancellationToken = default)
    {
        var predicate = await AdminPredicateAsync(query, username, messageType, cancellationToken);
        var page = (await _unitOfWork.ChatMessages.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, true, cancellationToken)).ToList();
        var total = await _unitOfWork.ChatMessages.CountForAdminAsync(predicate, cancellationToken);

        var userIds = page.SelectMany(e => new[] { e.SenderId, e.ReceiverId }).Distinct().ToList();
        var usernames = userIds.Count == 0
            ? new Dictionary<int, string?>()
            : (await _unitOfWork.Users.GetAllForAdminAsync(u => userIds.Contains(u.Id), cancellationToken)).ToDictionary(u => u.Id, u => (string?)u.Username);

        return PagedResult<AdminChatMessageDto>.Ok(page.Select(e => ToDecryptedAdminDto(e, usernames)), total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? username, string? messageType, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.ChatMessages.GetAdminStatusCountsAsync(await AdminPredicateAsync(query, username, messageType, cancellationToken), cancellationToken));
}
