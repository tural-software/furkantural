using System.Security.Claims;
using FurkanTural_Domain.Constants;

namespace FurkanTural_API.Helpers;

public static class AppTokenPrincipal
{
    public const string Role = "Visitor";

    public static string? AppSource(ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true
           && user.IsInRole(Role)
           && user.HasClaim(c => c.Type == ClaimDefinitions.AppKeyId)
            ? user.FindFirst(ClaimDefinitions.AppSource)?.Value
            : null;
}
