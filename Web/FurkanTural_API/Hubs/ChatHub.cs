using System.Security.Claims;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FurkanTural_API.Hubs;

[Authorize(Policy = "UserOrAdmin")]
public class ChatHub(
    IChatMessageService chatMessageService,
    ICallLogService callLogService,
    IUserFriendService userFriendService,
    ICallRateLimiter callRateLimiter,
    IPresenceTracker presenceTracker,
    IUserService userService) : Hub
{
    private readonly IChatMessageService _chatMessageService = chatMessageService;
    private readonly ICallLogService _callLogService = callLogService;
    private readonly IUserFriendService _userFriendService = userFriendService;
    private readonly ICallRateLimiter _callRateLimiter = callRateLimiter;
    private readonly IPresenceTracker _presenceTracker = presenceTracker;
    private readonly IUserService _userService = userService;

    private int? CurrentUserId()
    {
        var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    // ───────── Aktiflik (çevrimiçi / son görülme) ─────────

    /// <summary>Bağlanan kullanıcıyı çevrimiçi işaretler; arkadaşlarını bilgilendirir ve ona o an çevrimiçi olanları gönderir.</summary>
    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId();
        if (userId is not null)
        {
            var becameOnline = _presenceTracker.Connect(userId.Value, Context.ConnectionId);

            var friendsResult = await _userFriendService.GetFriendsAsync(userId.Value, Context.ConnectionAborted);
            var friends = friendsResult.Success && friendsResult.Data is not null
                ? friendsResult.Data.ToList()
                : [];

            // Bağlanan kullanıcıya o an çevrimiçi olan arkadaşlarının id listesini gönder.
            var onlineFriendIds = friends.Where(f => _presenceTracker.IsOnline(f.FriendUserId))
                                         .Select(f => f.FriendUserId)
                                         .ToArray();
            await Clients.Caller.SendAsync("OnlineFriends", onlineFriendIds);

            // İlk bağlantıysa arkadaşlarına "çevrimiçi oldu" bildir.
            if (becameOnline)
                foreach (var f in friends)
                    await Clients.User(f.FriendUserId.ToString()).SendAsync("UserOnline", userId.Value);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>Bağlantı kesilince; son bağlantıysa "son görülme" damgalanır ve arkadaşlar çevrimdışı bilgilendirilir.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId();
        if (userId is not null)
        {
            var becameOffline = _presenceTracker.Disconnect(userId.Value, Context.ConnectionId);
            if (becameOffline)
            {
                // Bağlantı koptuğu için Context.ConnectionAborted iptal edilmiş olabilir → None ile yaz.
                var lastSeen = await _userService.UpdateLastSeenAsync(userId.Value, CancellationToken.None);

                var friendsResult = await _userFriendService.GetFriendsAsync(userId.Value, CancellationToken.None);
                if (friendsResult.Success && friendsResult.Data is not null)
                    foreach (var f in friendsResult.Data)
                        await Clients.User(f.FriendUserId.ToString()).SendAsync("UserOffline", userId.Value, lastSeen);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ───────── Mesajlaşma ─────────

    /// <summary>Mesaj gönderir; arkadaşlık doğrulanır, kalıcılaştırılır ve alıcıya canlı iletilir.</summary>
    public async Task SendMessage(int receiverId, string content)
    {
        var senderId = CurrentUserId();
        if (senderId is null)
            return;

        var result = await _chatMessageService.SendAsync(senderId.Value, receiverId, content, Context.ConnectionAborted);
        if (result.Success && result.Data is not null)
        {
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", result.Data);
            await Clients.Caller.SendAsync("MessageSent", result.Data);
        }
        else
        {
            var error = result.Errors.Count > 0 ? result.Errors[0] : "Mesaj gönderilemedi.";
            await Clients.Caller.SendAsync("MessageError", error);
        }
    }

    /// <summary>"Yazıyor..." göstergesi için alıcıya bildirim gönderir.</summary>
    public async Task Typing(int receiverId)
    {
        var senderId = CurrentUserId();
        if (senderId is null)
            return;

        await Clients.User(receiverId.ToString()).SendAsync("UserTyping", senderId.Value);
    }

    // ───────── Sesli/Görüntülü arama (WebRTC sinyalleşme) ─────────

    /// <summary>Aramayı başlatır (offer). Arkadaşlık + engel + hız sınırı doğrular; callId döner.</summary>
    public async Task<int> CallUser(int receiverId, string callType, string offer)
    {
        var callerId = CurrentUserId();
        if (callerId is null)
            return 0;

        if (callerId.Value == receiverId)
        {
            await Clients.Caller.SendAsync("CallError", "Kendinizi arayamazsınız.");
            return 0;
        }

        if (!await _userFriendService.AreFriendsAsync(callerId.Value, receiverId, Context.ConnectionAborted))
        {
            await Clients.Caller.SendAsync("CallError", "Yalnızca arkadaşlarınızı arayabilirsiniz.");
            return 0;
        }

        if (await _userFriendService.IsBlockedBetweenAsync(callerId.Value, receiverId, Context.ConnectionAborted))
        {
            await Clients.Caller.SendAsync("CallError", "Bu kullanıcıyı arayamazsınız.");
            return 0;
        }

        if (!_callRateLimiter.TryStartCall(callerId.Value))
        {
            await Clients.Caller.SendAsync("CallError", "Çok sık arama başlattınız. Lütfen biraz bekleyin.");
            return 0;
        }

        var type = CallDefinitions.IsValidType(callType) ? callType : CallDefinitions.Types.Audio;
        var callId = await _callLogService.CreateRingingAsync(callerId.Value, receiverId, type, Context.ConnectionAborted);

        await Clients.User(receiverId.ToString()).SendAsync("IncomingCall", new
        {
            callId,
            callerId = callerId.Value,
            callType = type,
            offer
        });

        return callId;
    }

    /// <summary>Aramayı yanıtlar (answer). Yalnızca alıcı çağırabilir.</summary>
    public async Task AnswerCall(int callId, string answer)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null || call.CalleeId != userId.Value) return;

        await _callLogService.MarkAnsweredAsync(callId, Context.ConnectionAborted);
        await Clients.User(call.CallerId.ToString()).SendAsync("CallAnswered", new { callId, answer });
    }

    /// <summary>ICE adayını eşe iletir.</summary>
    public async Task SendIceCandidate(int callId, string candidate)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null) return;
        if (call.CallerId != userId.Value && call.CalleeId != userId.Value) return;

        var peerId = call.CallerId == userId.Value ? call.CalleeId : call.CallerId;
        await Clients.User(peerId.ToString()).SendAsync("ReceiveIceCandidate", new { callId, candidate });
    }

    /// <summary>Kamera aç/kapa durumunu eşe iletir (eşte avatar/video gösterimi için).</summary>
    public async Task NotifyMediaState(int callId, bool videoOn)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null) return;
        if (call.CallerId != userId.Value && call.CalleeId != userId.Value) return;

        var peerId = call.CallerId == userId.Value ? call.CalleeId : call.CallerId;
        await Clients.User(peerId.ToString()).SendAsync("CallMediaState", new { callId, videoOn });
    }

    /// <summary>Alıcı aramayı reddeder.</summary>
    public async Task RejectCall(int callId)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null || call.CalleeId != userId.Value) return;

        await _callLogService.MarkEndedAsync(callId, CallDefinitions.Statuses.Rejected, Context.ConnectionAborted);
        await Clients.User(call.CallerId.ToString()).SendAsync("CallRejected", new { callId });
    }

    /// <summary>Arayan, alıcı yanıtlamadan aramayı iptal eder (cevapsız).</summary>
    public async Task CancelCall(int callId)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null || call.CallerId != userId.Value) return;

        await _callLogService.MarkEndedAsync(callId, CallDefinitions.Statuses.Missed, Context.ConnectionAborted);
        await Clients.User(call.CalleeId.ToString()).SendAsync("CallCanceled", new { callId });
    }

    /// <summary>Görüşmeyi sonlandırır (her iki taraf da çağırabilir).</summary>
    public async Task HangUp(int callId)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null) return;
        if (call.CallerId != userId.Value && call.CalleeId != userId.Value) return;

        await _callLogService.MarkEndedAsync(callId, CallDefinitions.Statuses.Ended, Context.ConnectionAborted);
        var peerId = call.CallerId == userId.Value ? call.CalleeId : call.CallerId;
        await Clients.User(peerId.ToString()).SendAsync("CallEnded", new { callId });
    }
}
