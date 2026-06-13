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
/// <see cref="IPushSender"/>'ın Web Push uygulaması (VAPID). Tarayıcı push servislerine (FCM/Apple/Mozilla)
/// dışarı HTTPS POST atar — ayrı servis/Docker yok. "En iyi çaba": yapılandırma yoksa veya gönderim
/// başarısızsa sessizce geçer; geçersiz (404/410) abonelikleri temizler.
/// </summary>
public class PushSender(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<PushSender> logger) : IPushSender
{
    // HttpClient'i tek instance üzerinden paylaş (soket tükenmesini önler); thread-safe.
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
                return; // yapılandırılmamış → push pasif (mesajlaşma yine çalışır)

            var subs = (await _unitOfWork.PushSubscriptions.GetAllAsync(s => s.UserId == receiverUserId, cancellationToken)).ToList();
            if (subs.Count == 0)
                return;

            var payload = JsonSerializer.Serialize(new
            {
                title = "Chatural",
                body = $"{senderName} sana mesaj gönderdi",
                tag = "chat-message",   // aynı tag → bildirimler üst üste yığılmaz
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
                    dead.Add(s); // abonelik artık geçersiz → temizle
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
            // Push asla mesaj gönderimini bozmamalı.
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
