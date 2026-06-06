using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.DTOs.UserFriend;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FurkanTural_API.Realtime;

/// <summary>
/// <see cref="IChatNotifier"/>'ın SignalR uygulaması. Business katmanı bu soyutlamayı
/// kullanır; gerçek push işlemi <see cref="IHubContext{ChatHub}"/> üzerinden yapılır.
/// </summary>
public class ChatNotifier(IHubContext<ChatHub> hubContext) : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hubContext = hubContext;

    public Task NotifyFriendRequestReceivedAsync(int addresseeUserId, FriendRequestDto request)
        => _hubContext.Clients.User(addresseeUserId.ToString()).SendAsync("FriendRequestReceived", request);

    public Task NotifyFriendRequestAcceptedAsync(int requesterUserId, FriendDto friend)
        => _hubContext.Clients.User(requesterUserId.ToString()).SendAsync("FriendRequestAccepted", friend);

    public Task NotifyMessageReceivedAsync(int receiverUserId, ChatMessageDto message)
        => _hubContext.Clients.User(receiverUserId.ToString()).SendAsync("ReceiveMessage", message);

    public Task NotifyMessageReadAsync(int senderUserId, int byUserId)
        => _hubContext.Clients.User(senderUserId.ToString()).SendAsync("MessagesRead", byUserId);
}
