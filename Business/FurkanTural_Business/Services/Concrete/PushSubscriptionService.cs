using FurkanTural_Application.DTOs.Push;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace FurkanTural_Business.Services.Concrete;

public class PushSubscriptionService(IUnitOfWork unitOfWork, IConfiguration configuration) : IPushSubscriptionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IConfiguration _configuration = configuration;

    public async Task<Result> SubscribeAsync(int userId, PushSubscriptionDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Endpoint) || string.IsNullOrWhiteSpace(dto.P256dh) || string.IsNullOrWhiteSpace(dto.Auth))
            return Result.Fail("Geçersiz abonelik bilgisi.", statusCode: 400);

        if (!IsPushServiceEndpoint(dto.Endpoint!))
            return Result.Fail("Geçersiz abonelik bilgisi.", "Abonelik adresi push servisi adresi değil.", 400);

        var existing = await _unitOfWork.PushSubscriptions.GetAsync(s => s.Endpoint == dto.Endpoint, cancellationToken);
        if (existing is not null)
        {
            if (existing.UserId != userId
                && !(string.Equals(existing.P256dh, dto.P256dh, StringComparison.Ordinal)
                     && string.Equals(existing.Auth, dto.Auth, StringComparison.Ordinal)))
                return Result.Fail("Geçersiz abonelik bilgisi.",
                    $"Abonelik devri reddedildi: #{userId} başka bir hesabın aboneliğini cihaz anahtarları olmadan istedi.", 400);

            existing.UserId = userId;
            existing.P256dh = dto.P256dh!;
            existing.Auth = dto.Auth!;
            existing.UserAgent = dto.UserAgent;
            await _unitOfWork.PushSubscriptions.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await _unitOfWork.PushSubscriptions.AddAsync(new PushSubscription
            {
                UserId = userId,
                Endpoint = dto.Endpoint!,
                P256dh = dto.P256dh!,
                Auth = dto.Auth!,
                UserAgent = dto.UserAgent
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> UnsubscribeAsync(int userId, string? endpoint, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return Result.Ok();

        var entity = await _unitOfWork.PushSubscriptions.GetAsync(s => s.Endpoint == endpoint && s.UserId == userId, cancellationToken);
        if (entity is not null)
        {
            await _unitOfWork.PushSubscriptions.DeleteAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }

    public string? GetVapidPublicKey()
    {
        var key = _configuration["Push:Vapid:PublicKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Contains("####") || key.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            return null;
        return key;
    }

    private static bool IsPushServiceEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
            return false;

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return false;

        if (System.Net.IPAddress.TryParse(uri.Host, out var address))
            return !IsInternal(address);

        return true;
    }

    private static bool IsInternal(System.Net.IPAddress address)
    {
        if (System.Net.IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal)
            return true;

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return address.IsIPv4MappedToIPv6 && IsInternal(address.MapToIPv4());

        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            127 => true,
            169 when bytes[1] == 254 => true,
            172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
            192 when bytes[1] == 168 => true,
            _ => false
        };
    }
}