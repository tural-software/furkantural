using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.UserFriend;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Üye tarafındaki listeler kullanıcıları tek sorguda toplu çeker, satır başına arama yapmaz. Yönetim tarafı ise süzgeçsiz okumaya geçer: silinmiş veya pasif kullanıcıların kayıtları da listelendiği için normal okuma o satırlarda adı boş bırakırdı.</summary>
public class UserFriendService(
    IUnitOfWork unitOfWork,
    IStatusService statusService,
    IChatNotifier chatNotifier,
    IPresenceTracker presenceTracker,
    ActivityLogger activityLogger,
    IClock clock) : IUserFriendService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IStatusService _statusService = statusService;
    private readonly IChatNotifier _chatNotifier = chatNotifier;
    private readonly IPresenceTracker _presenceTracker = presenceTracker;
    private readonly ActivityLogger _activityLogger = activityLogger;
    private readonly IClock _clock = clock;

    private Task<int?> StatusIdAsync(string code, CancellationToken ct)
        => _statusService.GetIdByCodeAsync(StatusDefinitions.Groups.Friendship, code, ct);

    public async Task<Result> SendRequestAsync(int requesterId, int addresseeId, CancellationToken cancellationToken = default)
    {
        if (requesterId == addresseeId)
            return Result.Fail("Kendinizi arkadaş olarak ekleyemezsiniz.");

        var target = await _unitOfWork.Users.GetByIdAsync(addresseeId, cancellationToken);
        if (target is null)
            return Result.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        var pendingId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Pending, cancellationToken);
        var acceptedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Accepted, cancellationToken);
        var blockedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Blocked, cancellationToken);
        if (pendingId is null || acceptedId is null)
            return Result.Fail("Arkadaşlık statüleri yapılandırılmamış.", statusCode: 500);

        var existing = await _unitOfWork.UserFriends.GetAsync(
            x => (x.RequesterId == requesterId && x.AddresseeId == addresseeId)
              || (x.RequesterId == addresseeId && x.AddresseeId == requesterId),
            cancellationToken);

        if (existing is not null)
        {
            if (existing.StatusId == acceptedId)
                return Result.Fail("Bu kullanıcı zaten arkadaşınız.");
            if (blockedId is not null && existing.StatusId == blockedId)
                return Result.Fail("Bu kullanıcıya istek gönderemezsiniz.");
            if (existing.StatusId == pendingId)
            {
                return existing.AddresseeId == requesterId
                    ? Result.Fail("Bu kullanıcı size zaten bir arkadaşlık isteği göndermiş. İstekler sayfasından onaylayabilirsiniz.")
                    : Result.Fail("Bu kullanıcıya zaten bekleyen bir isteğiniz var.");
            }

            existing.RequesterId = requesterId;
            existing.AddresseeId = addresseeId;
            existing.StatusId = pendingId.Value;
            existing.RespondedAt = null;
            await _unitOfWork.UserFriends.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await NotifyRequestAsync(requesterId, addresseeId, existing.Id, existing.CreatedAt, cancellationToken);
            return Result.Ok();
        }

        var entity = new UserFriend
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            StatusId = pendingId.Value
        };
        await _unitOfWork.UserFriends.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Arkadaşlık isteği gönderildi. {requesterId} -> {addresseeId}", cancellationToken);
        await NotifyRequestAsync(requesterId, addresseeId, entity.Id, entity.CreatedAt, cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> AcceptRequestAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.UserFriends.GetByIdAsync(requestId, cancellationToken);
        if (entity is null)
            return Result.Fail("İstek bulunamadı.", statusCode: 404);

        var pendingId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Pending, cancellationToken);
        var acceptedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Accepted, cancellationToken);
        if (acceptedId is null)
            return Result.Fail("Arkadaşlık statüleri yapılandırılmamış.", statusCode: 500);

        if (entity.AddresseeId != currentUserId)
            return Result.Fail("Bu isteği onaylama yetkiniz yok.", statusCode: 403);
        if (entity.StatusId != pendingId)
            return Result.Fail("Bu istek zaten yanıtlanmış.");

        entity.StatusId = acceptedId.Value;
        entity.RespondedAt = _clock.UtcNow;
        await _unitOfWork.UserFriends.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Arkadaşlık isteği kabul edildi. Id: {entity.Id}", cancellationToken);

        var accepter = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (accepter is not null)
        {
            await _chatNotifier.NotifyFriendRequestAcceptedAsync(entity.RequesterId, new FriendDto
            {
                FriendshipId = entity.Id,
                FriendUserId = currentUserId,
                Username = accepter.Username,
                DisplayName = accepter.DisplayName,
                AvatarUrl = accepter.AvatarUrl,
                Since = entity.RespondedAt ?? _clock.UtcNow
            });
        }

        return Result.Ok();
    }

    public async Task<Result> RejectRequestAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.UserFriends.GetByIdAsync(requestId, cancellationToken);
        if (entity is null)
            return Result.Fail("İstek bulunamadı.", statusCode: 404);

        var pendingId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Pending, cancellationToken);
        var rejectedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Rejected, cancellationToken);
        if (rejectedId is null)
            return Result.Fail("Arkadaşlık statüleri yapılandırılmamış.", statusCode: 500);

        if (entity.AddresseeId != currentUserId)
            return Result.Fail("Bu isteği reddetme yetkiniz yok.", statusCode: 403);
        if (entity.StatusId != pendingId)
            return Result.Fail("Bu istek zaten yanıtlanmış.");

        entity.StatusId = rejectedId.Value;
        entity.RespondedAt = _clock.UtcNow;
        await _unitOfWork.UserFriends.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Arkadaşlık isteği reddedildi. Id: {entity.Id}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> RemoveFriendAsync(int currentUserId, int friendUserId, CancellationToken cancellationToken = default)
    {
        var acceptedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Accepted, cancellationToken);
        if (acceptedId is null)
            return Result.Fail("Arkadaşlık statüleri yapılandırılmamış.", statusCode: 500);

        var entity = await _unitOfWork.UserFriends.GetAsync(
            x => x.StatusId == acceptedId.Value &&
                ((x.RequesterId == currentUserId && x.AddresseeId == friendUserId) ||
                 (x.RequesterId == friendUserId && x.AddresseeId == currentUserId)),
            cancellationToken);

        if (entity is null)
            return Result.Fail("Arkadaşlık bulunamadı.", statusCode: 404);

        await _unitOfWork.UserFriends.SoftDeleteAsync(entity, currentUserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Arkadaşlık kaldırıldı. {currentUserId} - {friendUserId}", cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<FriendDto>>> GetFriendsAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var acceptedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Accepted, cancellationToken);
        if (acceptedId is null)
            return Result<IEnumerable<FriendDto>>.Ok([]);

        var relations = (await _unitOfWork.UserFriends.GetAllAsync(
            x => x.StatusId == acceptedId.Value && (x.RequesterId == currentUserId || x.AddresseeId == currentUserId),
            cancellationToken)).ToList();

        var users = await GetUsersByIdAsync(
            relations.Select(r => r.RequesterId == currentUserId ? r.AddresseeId : r.RequesterId),
            cancellationToken);

        var list = new List<FriendDto>();
        foreach (var relation in relations)
        {
            var otherId = relation.RequesterId == currentUserId ? relation.AddresseeId : relation.RequesterId;
            if (!users.TryGetValue(otherId, out var user))
                continue;

            list.Add(new FriendDto
            {
                FriendshipId = relation.Id,
                FriendUserId = otherId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Since = relation.RespondedAt ?? relation.CreatedAt,
                IsOnline = _presenceTracker.IsOnline(otherId),
                LastSeenAt = user.LastSeenAt
            });
        }

        return Result<IEnumerable<FriendDto>>.Ok(list.OrderBy(f => f.DisplayName ?? f.Username));
    }

    public async Task<Result<IEnumerable<FriendRequestDto>>> GetPendingRequestsAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var pendingId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Pending, cancellationToken);
        if (pendingId is null)
            return Result<IEnumerable<FriendRequestDto>>.Ok([]);

        var relations = (await _unitOfWork.UserFriends.GetAllAsync(
            x => x.StatusId == pendingId.Value && x.AddresseeId == currentUserId,
            cancellationToken)).ToList();

        var users = await GetUsersByIdAsync(relations.Select(r => r.RequesterId), cancellationToken);

        var list = new List<FriendRequestDto>();
        foreach (var relation in relations)
        {
            if (!users.TryGetValue(relation.RequesterId, out var user))
                continue;

            list.Add(new FriendRequestDto
            {
                RequestId = relation.Id,
                RequesterUserId = relation.RequesterId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                RequestedAt = relation.CreatedAt
            });
        }

        return Result<IEnumerable<FriendRequestDto>>.Ok(list.OrderByDescending(r => r.RequestedAt));
    }

    public async Task<Result<IEnumerable<FriendRequestDto>>> GetSentRequestsAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var pendingId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Pending, cancellationToken);
        if (pendingId is null)
            return Result<IEnumerable<FriendRequestDto>>.Ok([]);

        var relations = (await _unitOfWork.UserFriends.GetAllAsync(
            x => x.StatusId == pendingId.Value && x.RequesterId == currentUserId,
            cancellationToken)).ToList();

        var users = await GetUsersByIdAsync(relations.Select(r => r.AddresseeId), cancellationToken);

        var list = new List<FriendRequestDto>();
        foreach (var relation in relations)
        {
            if (!users.TryGetValue(relation.AddresseeId, out var user))
                continue;

            list.Add(new FriendRequestDto
            {
                RequestId = relation.Id,
                RequesterUserId = relation.RequesterId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                RequestedAt = relation.CreatedAt,
                IsOutgoing = true
            });
        }

        return Result<IEnumerable<FriendRequestDto>>.Ok(list.OrderByDescending(r => r.RequestedAt));
    }

    public async Task<Result<int>> GetPendingRequestCountAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var pendingId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Pending, cancellationToken);
        if (pendingId is null)
            return Result<int>.Ok(0);

        var count = await _unitOfWork.UserFriends.CountAsync(
            x => x.StatusId == pendingId.Value && x.AddresseeId == currentUserId,
            cancellationToken);

        return Result<int>.Ok(count);
    }

    public async Task<bool> AreFriendsAsync(int userA, int userB, CancellationToken cancellationToken = default)
    {
        var acceptedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Accepted, cancellationToken);
        if (acceptedId is null)
            return false;

        return await _unitOfWork.UserFriends.AnyAsync(
            x => x.StatusId == acceptedId.Value &&
                ((x.RequesterId == userA && x.AddresseeId == userB) ||
                 (x.RequesterId == userB && x.AddresseeId == userA)),
            cancellationToken);
    }

    public async Task<Result> BlockUserAsync(int currentUserId, int targetUserId, CancellationToken cancellationToken = default)
    {
        if (currentUserId == targetUserId)
            return Result.Fail("Kendinizi engelleyemezsiniz.");

        var target = await _unitOfWork.Users.GetByIdAsync(targetUserId, cancellationToken);
        if (target is null)
            return Result.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        var blockedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Blocked, cancellationToken);
        if (blockedId is null)
            return Result.Fail("Engelleme statüsü yapılandırılmamış.", statusCode: 500);

        var existing = await _unitOfWork.UserFriends.GetAsync(
            x => (x.RequesterId == currentUserId && x.AddresseeId == targetUserId)
              || (x.RequesterId == targetUserId && x.AddresseeId == currentUserId),
            cancellationToken);

        if (existing is not null)
        {
            existing.RequesterId = currentUserId;
            existing.AddresseeId = targetUserId;
            existing.StatusId = blockedId.Value;
            existing.BlockedByUserId = currentUserId;
            existing.RespondedAt = _clock.UtcNow;
            await _unitOfWork.UserFriends.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await _unitOfWork.UserFriends.AddAsync(new UserFriend
            {
                RequesterId = currentUserId,
                AddresseeId = targetUserId,
                StatusId = blockedId.Value,
                BlockedByUserId = currentUserId,
                RespondedAt = _clock.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Kullanıcı engellendi. {currentUserId} -> {targetUserId}", cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> UnblockUserAsync(int currentUserId, int targetUserId, CancellationToken cancellationToken = default)
    {
        var blockedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Blocked, cancellationToken);
        if (blockedId is null)
            return Result.Fail("Engelleme statüsü yapılandırılmamış.", statusCode: 500);

        var entity = await _unitOfWork.UserFriends.GetAsync(
            x => x.StatusId == blockedId.Value && x.BlockedByUserId == currentUserId &&
                ((x.RequesterId == currentUserId && x.AddresseeId == targetUserId) ||
                 (x.RequesterId == targetUserId && x.AddresseeId == currentUserId)),
            cancellationToken);

        if (entity is null)
            return Result.Fail("Engellenen kullanıcı bulunamadı.", statusCode: 404);

        await _unitOfWork.UserFriends.SoftDeleteAsync(entity, currentUserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Engel kaldırıldı. {currentUserId} -> {targetUserId}", cancellationToken);
        return Result.Ok();
    }

    public async Task<bool> IsBlockedBetweenAsync(int userA, int userB, CancellationToken cancellationToken = default)
    {
        var blockedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Blocked, cancellationToken);
        if (blockedId is null)
            return false;

        return await _unitOfWork.UserFriends.AnyAsync(
            x => x.StatusId == blockedId.Value &&
                ((x.RequesterId == userA && x.AddresseeId == userB) ||
                 (x.RequesterId == userB && x.AddresseeId == userA)),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<FriendDto>>> GetBlockedAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var blockedId = await StatusIdAsync(StatusDefinitions.FriendshipCodes.Blocked, cancellationToken);
        if (blockedId is null)
            return Result<IEnumerable<FriendDto>>.Ok([]);

        var relations = (await _unitOfWork.UserFriends.GetAllAsync(
            x => x.StatusId == blockedId.Value && x.BlockedByUserId == currentUserId,
            cancellationToken)).ToList();

        var users = await GetUsersByIdAsync(
            relations.Select(r => r.RequesterId == currentUserId ? r.AddresseeId : r.RequesterId),
            cancellationToken);

        var list = new List<FriendDto>();
        foreach (var relation in relations)
        {
            var otherId = relation.RequesterId == currentUserId ? relation.AddresseeId : relation.RequesterId;
            if (!users.TryGetValue(otherId, out var user))
                continue;

            list.Add(new FriendDto
            {
                FriendshipId = relation.Id,
                FriendUserId = otherId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Since = relation.RespondedAt ?? relation.CreatedAt,
                IsOnline = _presenceTracker.IsOnline(otherId),
                LastSeenAt = user.LastSeenAt
            });
        }

        return Result<IEnumerable<FriendDto>>.Ok(list.OrderBy(f => f.DisplayName ?? f.Username));
    }

    public async Task<Result<IEnumerable<AdminUserFriendDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.UserFriends.GetAllForAdminAsync(cancellationToken);
        var statuses = (await _unitOfWork.Statuses.GetAllForAdminAsync(cancellationToken)).ToDictionary(s => s.Id);

        var users = (await _unitOfWork.Users.GetAllForAdminAsync(cancellationToken)).ToDictionary(u => u.Id);

        var dtos = entities.Select(e =>
        {
            var dto = e.ToAdminDto();
            if (statuses.TryGetValue(e.StatusId, out var status))
            {
                dto.StatusCode = status.Code;
                dto.StatusName = status.Name;
            }
            if (users.TryGetValue(e.RequesterId, out var requester))
                dto.RequesterUsername = requester.Username;
            if (users.TryGetValue(e.AddresseeId, out var addressee))
                dto.AddresseeUsername = addressee.Username;
            return dto;
        });

        return Result<IEnumerable<AdminUserFriendDto>>.Ok(dtos);
    }

    public async Task<Result<AdminUserFriendDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.UserFriends.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminUserFriendDto>.Fail("Arkadaşlık kaydı bulunamadı.", statusCode: 404);

        return Result<AdminUserFriendDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<AdminUserFriendDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.UserFriends.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminUserFriendDto>.Fail("Arkadaşlık kaydı bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminUserFriendDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.UserFriends.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminUserFriendDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<AdminUserFriendDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.UserFriends.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminUserFriendDto>.Fail("Arkadaşlık kaydı bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminUserFriendDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.UserFriends.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminUserFriendDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.UserFriends.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private async Task<Dictionary<int, User>> GetUsersByIdAsync(IEnumerable<int> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var users = await _unitOfWork.Users.GetAllAsync(u => ids.Contains(u.Id), cancellationToken);
        return users.ToDictionary(u => u.Id);
    }

    private async Task NotifyRequestAsync(int requesterId, int addresseeId, int requestId, DateTime requestedAt, CancellationToken cancellationToken)
    {
        var requester = await _unitOfWork.Users.GetByIdAsync(requesterId, cancellationToken);
        if (requester is null)
            return;

        await _chatNotifier.NotifyFriendRequestReceivedAsync(addresseeId, new FriendRequestDto
        {
            RequestId = requestId,
            RequesterUserId = requesterId,
            Username = requester.Username,
            DisplayName = requester.DisplayName,
            AvatarUrl = requester.AvatarUrl,
            RequestedAt = requestedAt
        });
    }

    private async Task<AdminUserFriendDto> EnrichAsync(UserFriend entity, CancellationToken cancellationToken)
    {
        var dto = entity.ToAdminDto();
        var status = await _unitOfWork.Statuses.GetByIdForAdminAsync(entity.StatusId, cancellationToken);
        dto.StatusCode = status?.Code;
        dto.StatusName = status?.Name;

        dto.RequesterUsername = (await _unitOfWork.Users.GetByIdForAdminAsync(entity.RequesterId, cancellationToken))?.Username;
        dto.AddresseeUsername = (await _unitOfWork.Users.GetByIdForAdminAsync(entity.AddresseeId, cancellationToken))?.Username;

        return dto;
    }
}
