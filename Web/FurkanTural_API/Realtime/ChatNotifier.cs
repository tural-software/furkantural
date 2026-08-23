using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.DTOs.UserFriend;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FurkanTural_API.Realtime;

/// <summary>Hedefleme bağlantıya değil kullanıcıya yapılır, dolayısıyla bildirim o kullanıcının açık olan bütün sekme ve cihazlarına birden gider. Business katmanının SignalR'a bağlanmadan bildirim gönderebilmesi bu sınıf sayesindedir; soyutlama orada, uygulaması burada durur.</summary>
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

    public Task NotifyMessageDeletedAsync(int targetUserId, ChatMessageDto message)
        => _hubContext.Clients.User(targetUserId.ToString()).SendAsync("MessageDeleted", message);

    public Task NotifyMessageEditedAsync(int targetUserId, ChatMessageDto message)
        => _hubContext.Clients.User(targetUserId.ToString()).SendAsync("MessageEdited", message);
}
