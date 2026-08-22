using System.Text.Json;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;
using DomainPushSubscription = FurkanTural_Domain.Entities.PushSubscription;
using LibPushSubscription = WebPush.PushSubscription;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// VAPID ile Web Push. Anahtarlar <c>Push:Vapid</c> altındaki Subject, PublicKey ve PrivateKey'den
/// okunur; değer boşsa ya da yer tutucu deseni taşıyorsa push kapalı sayılır. Gönderim tarayıcının
/// kendi push servisine dışarı HTTPS isteğiyle yapılır, araya ayrı bir servis girmez.
///
/// Push istemcisi süreç boyunca tek örnektir — abonelik başına yeni istemci soket tüketirdi. 404 veya
/// 410 dönen abonelikler tarayıcı tarafında düşmüş demektir ve aynı tur içinde silinir; diğer hatalar
/// yalnızca günlüğe yazılır.
/// </summary>
public class PushSender(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<PushSender> logger) : IPushSender
{
    private static readonly WebPushClient _client = new();

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<PushSender> _logger = logger;

    public async Task SendMessageNotificationAsync(int receiverUserId, string senderName, CancellationToken cancellationToken = default)
    {
        try
        {
            var vapid = ReadVapid();
            if (vapid is null)
                return;

            var subs = (await _unitOfWork.PushSubscriptions.GetAllAsync(s => s.UserId == receiverUserId, cancellationToken)).ToList();
            if (subs.Count == 0)
                return;

            var payload = JsonSerializer.Serialize(new
            {
                title = "Chatural",
                body = $"{senderName} sana mesaj gönderdi",
                tag = "chat-message",
                url = "/Chat"
            });

            var dead = new List<DomainPushSubscription>();
            foreach (var s in subs)
            {
                try
                {
                    await _client.SendNotificationAsync(new LibPushSubscription(s.Endpoint, s.P256dh, s.Auth), payload, vapid);
                }
                catch (WebPushException ex) when ((int)ex.StatusCode is 404 or 410)
                {
                    dead.Add(s);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Push bildirimi gönderilemedi. Abonelik: {Id}", s.Id);
                }
            }

            if (dead.Count > 0)
            {
                await _unitOfWork.PushSubscriptions.DeleteRangeAsync(dead, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push bildirimi akışında beklenmeyen hata. Alıcı: {UserId}", receiverUserId);
        }
    }

    private VapidDetails? ReadVapid()
    {
        var subject = _configuration["Push:Vapid:Subject"];
        var publicKey = _configuration["Push:Vapid:PublicKey"];
        var privateKey = _configuration["Push:Vapid:PrivateKey"];

        if (IsMissing(subject) || IsMissing(publicKey) || IsMissing(privateKey))
            return null;

        return new VapidDetails(subject, publicKey, privateKey);
    }

    private static bool IsMissing(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Contains("####") || value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
}