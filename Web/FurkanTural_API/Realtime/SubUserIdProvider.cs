using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FurkanTural_API.Realtime;

/// <summary>SignalR'ın <c>Clients.User(id)</c> hedeflemesini JWT'deki kullanıcı Id'sine bağlar. JWT claim eşlemesine göre Id, <c>NameIdentifier</c> veya <c>sub</c> altında olabilir.</summary>
public class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
