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

public class ChatMessageService(
    IUnitOfWork unitOfWork,
    IUserFriendService userFriendService,
    ActivityLogger activityLogger,
    IClock clock) : IChatMessageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserFriendService _userFriendService = userFriendService;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly IClock _clock = clock;

    // ── Üye işlemleri ──

    public async Task<Result<ChatMessageDto>> SendAsync(int senderId, int receiverId, string? content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result<ChatMessageDto>.Fail("Mesaj boş olamaz.");

        if (senderId == receiverId)
            return Result<ChatMessageDto>.Fail("Kendinize mesaj gönderemezsiniz.");

        var areFriends = await _userFriendService.AreFriendsAsync(senderId, receiverId, cancellationToken);
        if (!areFriends)
            return Result<ChatMessageDto>.Fail("Yalnızca arkadaşlarınıza mesaj gönderebilirsiniz.", statusCode: 403);

        if (await _userFriendService.IsBlockedBetweenAsync(senderId, receiverId, cancellationToken))
            return Result<ChatMessageDto>.Fail("Bu kullanıcıyla mesajlaşamazsınız.", statusCode: 403);

        var entity = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content.Trim(),
            MessageType = "Text",
            IsRead = false
        };
        await _unitOfWork.ChatMessages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChatMessageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<ChatMessageDto>> SendAudioAsync(int senderId, int receiverId, string? fileName, int? durationSeconds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result<ChatMessageDto>.Fail("Ses dosyası gerekli.");

        if (senderId == receiverId)
            return Result<ChatMessageDto>.Fail("Kendinize mesaj gönderemezsiniz.");

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

        var areFriends = await _userFriendService.AreFriendsAsync(senderId, receiverId, cancellationToken);
        if (!areFriends)
            return Result<ChatMessageDto>.Fail("Yalnızca arkadaşlarınıza mesaj gönderebilirsiniz.", statusCode: 403);

        if (await _userFriendService.IsBlockedBetweenAsync(senderId, receiverId, cancellationToken))
            return Result<ChatMessageDto>.Fail("Bu kullanıcıyla mesajlaşamazsınız.", statusCode: 403);

        var entity = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            // Image -> "Image", Video -> "Video" (normalize)
            MessageType = ChatMessageTypes.IsMedia(messageType) && string.Equals(messageType, ChatMessageTypes.Video, StringComparison.OrdinalIgnoreCase)
                ? ChatMessageTypes.Video : ChatMessageTypes.Image,
            AttachmentUrl = fileName,
            DurationSeconds = durationSeconds,
            IsRead = false
        };
        await _unitOfWork.ChatMessages.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChatMessageDto>.Ok(entity.ToDto());
    }

    public async Task<Result<IEnumerable<ChatMessageDto>>> GetConversationAsync(int currentUserId, int otherUserId, int? take = null, CancellationToken cancellationToken = default)
    {
        var areFriends = await _userFriendService.AreFriendsAsync(currentUserId, otherUserId, cancellationToken);
        if (!areFriends)
            return Result<IEnumerable<ChatMessageDto>>.Fail("Yalnızca arkadaşlarınızla olan sohbeti görüntüleyebilirsiniz.", statusCode: 403);

        var messages = await _unitOfWork.ChatMessages.GetAllAsync(
            x => (x.SenderId == currentUserId && x.ReceiverId == otherUserId) ||
                 (x.SenderId == otherUserId && x.ReceiverId == currentUserId),
            cancellationToken);

        var ordered = messages.OrderBy(m => m.CreatedAt).AsEnumerable();
        if (take is > 0)
            ordered = ordered.TakeLast(take.Value);

        return Result<IEnumerable<ChatMessageDto>>.Ok(ordered.Select(m => m.ToDto()));
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

        var summaries = new List<ConversationSummaryDto>();
        foreach (var friend in friends)
        {
            var messages = await _unitOfWork.ChatMessages.GetAllAsync(
                x => (x.SenderId == currentUserId && x.ReceiverId == friend.FriendUserId) ||
                     (x.SenderId == friend.FriendUserId && x.ReceiverId == currentUserId),
                cancellationToken);

            var messageList = messages.ToList();
            var last = messageList.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var unread = messageList.Count(m => m.SenderId == friend.FriendUserId && m.ReceiverId == currentUserId && !m.IsRead);

            summaries.Add(new ConversationSummaryDto
            {
                FriendUserId = friend.FriendUserId,
                Username = friend.Username,
                DisplayName = friend.DisplayName,
                AvatarUrl = friend.AvatarUrl,
                LastMessage = last?.Content,
                LastMessageAt = last?.CreatedAt,
                UnreadCount = unread
            });
        }

        return Result<IEnumerable<ConversationSummaryDto>>.Ok(summaries.OrderByDescending(s => s.LastMessageAt ?? DateTime.MinValue));
    }

    // ── Admin ──

    public async Task<Result<IEnumerable<AdminChatMessageDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.ChatMessages.GetAllForAdminAsync(cancellationToken);
        return Result<IEnumerable<AdminChatMessageDto>>.Ok(entities.OrderByDescending(e => e.CreatedAt).Select(e => e.ToAdminDto()));
    }

    public async Task<PagedResult<AdminChatMessageDto>> GetAllPagedForAdminAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = (await _unitOfWork.ChatMessages.GetAllForAdminAsync(cancellationToken))
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        var page = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(e => e.ToAdminDto());
        return PagedResult<AdminChatMessageDto>.Ok(page, all.Count, pageNumber, pageSize);
    }

    public async Task<Result<AdminChatMessageDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ChatMessages.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminChatMessageDto>.Fail("Mesaj bulunamadı.", statusCode: 404);

        return Result<AdminChatMessageDto>.Ok(entity.ToAdminDto());
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

        return Result<AdminChatMessageDto>.Ok(entity.ToAdminDto());
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

        return Result<AdminChatMessageDto>.Ok(entity.ToAdminDto());
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.ChatMessages.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }
}
