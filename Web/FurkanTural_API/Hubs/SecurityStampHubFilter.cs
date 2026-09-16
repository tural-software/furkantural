using System.Security.Claims;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Constants;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FurkanTural_API.Hubs;

/// <summary>Jeton yalnızca bağlantı kurulurken doğrulanıyordu; açık kalan bir WebSocket, kullanıcı yasaklansa, silinse ya da rolü düşürülse bile çalışmaya devam ediyordu. Bu süzgeç bağlantının açılışında ve her hub çağrısında jetonun güvenlik damgasını veri tabanındakine karşı sınar; eşleşmezse bağlantıyı kapatır.<para>Her çağrı bir veri tabanı okuması demektir. Bu bilinçli bir bedeldir: yasaklanan kullanıcının açık sekmesinden mesaj atıp arama başlatabilmesi, birkaç milisaniyelik gecikmeden daha pahalıdır.</para><para>Bağlantı kapandığında istemci kendiliğinden yeniden bağlanmayı dener; yeniden bağlanma BFF üzerinden taze jetonla yapılır ve hesap hâlâ geçerliyse kullanıcı hiçbir şey fark etmez.</para></summary>
public sealed class SecurityStampHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (!await IsCurrentAsync(invocationContext.Context.User, invocationContext.ServiceProvider, invocationContext.Context.ConnectionAborted))
        {
            invocationContext.Context.Abort();
            throw new HubException("Oturum geçersiz.");
        }

        return await next(invocationContext);
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (!await IsCurrentAsync(context.Context.User, context.ServiceProvider, context.Context.ConnectionAborted))
            throw new HubException("Oturum geçersiz.");

        await next(context);
    }

    private static async Task<bool> IsCurrentAsync(ClaimsPrincipal? user, IServiceProvider services, CancellationToken cancellationToken)
    {
        var sub = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var stamp = user?.FindFirst(ClaimDefinitions.SecurityStamp)?.Value;

        if (!int.TryParse(sub, out var userId) || string.IsNullOrEmpty(stamp))
            return false;

        var account = await services.GetRequiredService<IUnitOfWork>().Users.GetByIdAsync(userId, cancellationToken);
        return account is not null && string.Equals(account.SecurityStamp, stamp, StringComparison.Ordinal);
    }
}
