using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace FurkanTural_Blog;

public static class ClientForwarding
{
    public const string IpHeader = "X-FT-Client-IP";
    public const string UserAgentHeader = "X-FT-Client-UA";

    public static void Apply(HttpContext? context, HttpRequestHeaders headers)
    {
        headers.Remove(IpHeader);
        headers.Remove(UserAgentHeader);

        if (context is null)
            return;

        var ip = context.Connection.RemoteIpAddress;
        if (ip is not null)
            headers.TryAddWithoutValidation(IpHeader, (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString());

        var userAgent = context.Request.Headers["User-Agent"].ToString();
        if (userAgent.Length > 0 && userAgent.All(c => c >= 0x20 && c < 0x7F))
            headers.TryAddWithoutValidation(UserAgentHeader, userAgent);
    }
}

public class ClientForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ClientForwarding.Apply(_httpContextAccessor.HttpContext, request.Headers);
        return base.SendAsync(request, cancellationToken);
    }
}
