using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.DTOs.UserFriend;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>
/// Gerçek zamanlı bildirim soyutlaması. Business katmanı SignalR'a doğrudan bağımlı
/// olmasın diye burada tanımlanır; somut uygulaması (IHubContext) API katmanındadır.
/// </summary>
public interface IChatNotifier
{
    Task NotifyFriendRequestReceivedAsync(int addresseeUserId, FriendRequestDto request);
    Task NotifyFriendRequestAcceptedAsync(int requesterUserId, FriendDto friend);
    Task NotifyMessageReceivedAsync(int receiverUserId, ChatMessageDto message);
    Task NotifyMessageReadAsync(int senderUserId, int byUserId);
}
