using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.UserFriend;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IUserFriendService
{
    // ── Üye işlemleri ──
    Task<Result> SendRequestAsync(int requesterId, int addresseeId, CancellationToken cancellationToken = default);
    Task<Result> AcceptRequestAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default);
    Task<Result> RejectRequestAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default);
    Task<Result> RemoveFriendAsync(int currentUserId, int friendUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FriendDto>>> GetFriendsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FriendRequestDto>>> GetPendingRequestsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FriendRequestDto>>> GetSentRequestsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<int>> GetPendingRequestCountAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<bool> AreFriendsAsync(int userA, int userB, CancellationToken cancellationToken = default);

    // ── Engelleme (block) ──
    Task<Result> BlockUserAsync(int currentUserId, int targetUserId, CancellationToken cancellationToken = default);
    Task<Result> UnblockUserAsync(int currentUserId, int targetUserId, CancellationToken cancellationToken = default);
    Task<bool> IsBlockedBetweenAsync(int userA, int userB, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FriendDto>>> GetBlockedAsync(int currentUserId, CancellationToken cancellationToken = default);

    // ── Admin ──
    Task<Result<IEnumerable<AdminUserFriendDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminUserFriendDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminUserFriendDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminUserFriendDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
