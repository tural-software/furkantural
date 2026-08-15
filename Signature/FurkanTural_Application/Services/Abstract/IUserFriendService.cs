using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.UserFriend;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Arkadaşlık ve engelleme. Bir kullanıcı çifti için tabloda tek satır tutulur ve o satır yeniden
/// kullanılır: engelleme yeni kayıt açmaz, mevcut ilişkiyi Blocked statüsüne çevirip engelleyeni
/// Requester tarafına taşır. UnblockUserAsync yalnızca engeli koyan tarafça çağrılabilir ve satırı
/// yumuşak siler, yani ilişki sıfırlanır; taraflar baştan istek gönderebilir. Statüler Id ile değil
/// <see cref="FurkanTural_Domain.Constants.StatusDefinitions"/> içindeki Group ve Code ikilisiyle
/// çözülür. AreFriendsAsync ile IsBlockedBetweenAsync <see cref="Wrappers.Result"/> zarfı kullanmaz;
/// statü satırı eksikse hata yerine false dönerler, dolayısıyla false "ilişki yok" ile "sistem
/// yapılandırılmamış" durumlarını ayırmaz.
/// </summary>
public interface IUserFriendService
{
    Task<Result> SendRequestAsync(int requesterId, int addresseeId, CancellationToken cancellationToken = default);
    Task<Result> AcceptRequestAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default);
    Task<Result> RejectRequestAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default);
    Task<Result> RemoveFriendAsync(int currentUserId, int friendUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FriendDto>>> GetFriendsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FriendRequestDto>>> GetPendingRequestsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FriendRequestDto>>> GetSentRequestsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<int>> GetPendingRequestCountAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<bool> AreFriendsAsync(int userA, int userB, CancellationToken cancellationToken = default);
    Task<Result> BlockUserAsync(int currentUserId, int targetUserId, CancellationToken cancellationToken = default);
    Task<Result> UnblockUserAsync(int currentUserId, int targetUserId, CancellationToken cancellationToken = default);
    Task<bool> IsBlockedBetweenAsync(int userA, int userB, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FriendDto>>> GetBlockedAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminUserFriendDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminUserFriendDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminUserFriendDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminUserFriendDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}