using FurkanTural_Application.DTOs.ChatMessage;
using FurkanTural_Application.DTOs.UserFriend;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Gerçek zamanlı sohbet bildirimlerinin soyutlaması; iş katmanı gerçek zamanlı altyapıya doğrudan bağlanmasın diye vardır, uygulaması sunum tarafında durur. Hedefleme bağlantı kimliğiyle değil kullanıcı kimliğiyle yapılır, yani kullanıcının açık tüm oturumları aynı bildirimi alır. Teslim garantisi yoktur: alıcı çevrim dışıysa bildirim düşer, kalıcı olan yalnızca veri tabanına yazılan kayıttır — bu yüzden bildirim gönderimi kayıt işleminin yerine geçmez.</summary>
public interface IChatNotifier
{
    Task NotifyFriendRequestReceivedAsync(int addresseeUserId, FriendRequestDto request);
    Task NotifyFriendRequestAcceptedAsync(int requesterUserId, FriendDto friend);
    Task NotifyMessageReceivedAsync(int receiverUserId, ChatMessageDto message);
    Task NotifyMessageReadAsync(int senderUserId, int byUserId);
    Task NotifyMessageDeletedAsync(int targetUserId, ChatMessageDto message);
    Task NotifyMessageEditedAsync(int targetUserId, ChatMessageDto message);
}
