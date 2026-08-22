using System.Security.Claims;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FurkanTural_API.Hubs;

/// <summary>
/// Çağıran her metotta jetondan çözülür. Kimlik çözülemezse metot hata üretmeden sessizce döner:
/// istemci bir yanıt bekliyorsa cevapsız kalır, beklemiyorsa hiçbir şey olmamış gibi görünür.
///
/// Hata bildirimi istisnayla değil istemci olayıyla yapılır. Mesajlaşmada <c>MessageError</c>,
/// aramada <c>CallError</c> olayı çağırana geri gönderilir; hub metodu yine başarıyla tamamlanır.
///
/// Arama metotlarının yetkisi taraf olmaya bağlıdır ve her biri farklıdır: yanıtlama ile reddetmeyi
/// yalnızca aranan, iptali yalnızca arayan yapabilir, kapatmayı ise iki taraf da yapabilir. Taraf
/// olmayan çağrı sessizce düşer.
/// </summary>
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

    /// <summary>
    /// Bağlanana o an çevrimiçi olan arkadaşlarının listesi tek seferde gönderilir; istemci
    /// açılışta kimin çevrimiçi olduğunu ayrıca sormaz. Arkadaşlara "çevrimiçi oldu" bildirimi
    /// yalnızca ilk bağlantıda gider, ikinci sekme açıldığında tekrarlanmaz.
    /// </summary>
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

            var onlineFriendIds = friends.Where(f => _presenceTracker.IsOnline(f.FriendUserId))
                                         .Select(f => f.FriendUserId)
                                         .ToArray();
            await Clients.Caller.SendAsync("OnlineFriends", onlineFriendIds);

            if (becameOnline)
                foreach (var f in friends)
                    await Clients.User(f.FriendUserId.ToString()).SendAsync("UserOnline", userId.Value);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Son görülme damgası ve çevrimdışı bildirimi yalnızca kullanıcının son bağlantısı kapandığında
    /// üretilir; açık başka sekmesi varsa hiçbiri olmaz.
    ///
    /// Buradaki yazmalar isteğin iptal jetonuyla değil iptal edilemez bir jetonla yapılır: bağlantı
    /// zaten koptuğu için istek jetonu iptal edilmiş durumdadır ve onunla yazmak son görülme
    /// damgasını hiç kaydettirmezdi.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId();
        if (userId is not null)
        {
            var becameOffline = _presenceTracker.Disconnect(userId.Value, Context.ConnectionId);
            if (becameOffline)
            {
                var lastSeen = await _userService.UpdateLastSeenAsync(userId.Value, CancellationToken.None);

                var friendsResult = await _userFriendService.GetFriendsAsync(userId.Value, CancellationToken.None);
                if (friendsResult.Success && friendsResult.Data is not null)
                    foreach (var f in friendsResult.Data)
                        await Clients.User(f.FriendUserId.ToString()).SendAsync("UserOffline", userId.Value, lastSeen);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

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

    /// <summary>
    /// Arkadaşlık burada ayrıca doğrulanır. Yalnızca gürültüyü kesmek için değil: doğrulama olmasa
    /// rastgele bir kimliğe yazma sinyali gönderip yanıtına bakarak o hesabın var olup olmadığı
    /// anlaşılabilirdi.
    /// </summary>
    public async Task Typing(int receiverId)
    {
        var senderId = CurrentUserId();
        if (senderId is null)
            return;

        if (!await _userFriendService.AreFriendsAsync(senderId.Value, receiverId, Context.ConnectionAborted))
            return;

        await Clients.User(receiverId.ToString()).SendAsync("UserTyping", senderId.Value);
    }

    /// <summary>
    /// WebRTC sinyalleşmesinin giriş kapısı. Dört koşul sırayla denetlenir: kendini arama, arkadaşlık,
    /// engel ve hız sınırı. Hız sınırı en sonda durur, çünkü geçen her çağrı kotadan düşer; önce
    /// denetlenseydi zaten reddedilecek çağrılar da kotayı tüketirdi.
    ///
    /// Dönen değer arama kimliğidir ve sonraki bütün sinyal metotları onu bekler. Koşullardan biri
    /// tutmazsa hata <c>CallError</c> olayıyla gider ve dönüş sıfır olur; çağıran bunu geçerli bir
    /// kimlik sanmamalıdır.
    /// </summary>
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

    /// <summary>
    /// Yalnızca hâlâ çalan bir arama yanıtlanabilir. Bu denetim yarışa ve yeniden oynatmaya karşıdır:
    /// iptal edilmiş, reddedilmiş veya bitmiş bir arama, eski bir yanıt paketi tekrar gönderilerek
    /// canlandırılamaz.
    /// </summary>
    public async Task AnswerCall(int callId, string answer)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null || call.CalleeId != userId.Value) return;

        if (!string.Equals(call.Status, CallDefinitions.Statuses.Ringing, StringComparison.OrdinalIgnoreCase)) return;

        await _callLogService.MarkAnsweredAsync(callId, Context.ConnectionAborted);
        await Clients.User(call.CallerId.ToString()).SendAsync("CallAnswered", new { callId, answer });
    }

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

    public async Task RejectCall(int callId)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null || call.CalleeId != userId.Value) return;

        await _callLogService.MarkEndedAsync(callId, CallDefinitions.Statuses.Rejected, Context.ConnectionAborted);
        await Clients.User(call.CallerId.ToString()).SendAsync("CallRejected", new { callId });
    }

    /// <summary>
    /// Arayan vazgeçtiğinde kayda "iptal edildi" değil "cevapsız" yazılır. Arananın gözünden olan da
    /// budur; arama kayıtlarında bu satırlar kaçırılmış aramalarla aynı kovada görünür.
    /// </summary>
    public async Task CancelCall(int callId)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var call = await _callLogService.GetParticipantsAsync(callId, Context.ConnectionAborted);
        if (call is null || call.CallerId != userId.Value) return;

        await _callLogService.MarkEndedAsync(callId, CallDefinitions.Statuses.Missed, Context.ConnectionAborted);
        await Clients.User(call.CalleeId.ToString()).SendAsync("CallCanceled", new { callId });
    }

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