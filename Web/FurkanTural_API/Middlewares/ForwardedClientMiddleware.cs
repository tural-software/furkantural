using System.Net;
using System.Security.Claims;
using FurkanTural_API.Helpers;
using FurkanTural_Domain.Constants;

namespace FurkanTural_API.Middlewares;

public sealed class ForwardedClientMiddleware(RequestDelegate next)
{
    public const string IpHeader = "X-FT-Client-IP";
    public const string UserAgentHeader = "X-FT-Client-UA";
    public const int MaxUserAgentLength = 512;

    private static readonly HashSet<string> TrustedApps = new(StringComparer.Ordinal)
    {
        AppSourceDefinitions.Portfolio,
        AppSourceDefinitions.Blog,
        AppSourceDefinitions.Chat,
        AppSourceDefinitions.Admin
    };

    private readonly RequestDelegate _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        if (IsTrustedCaller(context.User))
        {
            var rawIp = context.Request.Headers[IpHeader].ToString().Trim();
            if ((rawIp.Contains('.') || rawIp.Contains(':')) && IPAddress.TryParse(rawIp, out var ip))
                context.Connection.RemoteIpAddress = ip;

            var userAgent = context.Request.Headers[UserAgentHeader].ToString();
            if (userAgent.Length > 0)
                context.Request.Headers.UserAgent = userAgent.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent;
        }

        return _next(context);
    }

    private static bool IsTrustedCaller(ClaimsPrincipal user)
    {
        var app = AppTokenPrincipal.AppSource(user);
        return app is not null && TrustedApps.Contains(app);
    }
}
