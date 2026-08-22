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

        var existing = await _unitOfWork.PushSubscriptions.GetAsync(s => s.Endpoint == dto.Endpoint, cancellationToken);
        if (existing is not null)
        {
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
}