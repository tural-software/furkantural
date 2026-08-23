using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace FurkanTural_Admin.Tests.Infrastructure;

/// <summary>Controller testleri için HttpContext, Session ve UrlHelper stublarını tek noktadan hazırlayan yardımcı.</summary>
public static class ControllerTestHelper
{
    /// <summary>Verilen token değeriyle (null ise boş session) bir ControllerContext hazırlar. Controller'a atanmaya hazır döner.</summary>
    public static ControllerContext BuildControllerContext(string? sessionToken)
    {
        var session = new MockSession();
        if (!string.IsNullOrEmpty(sessionToken))
            session.SetString("token", sessionToken);

        var httpContext = new DefaultHttpContext
        {
            Session = session
        };

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    /// <summary>Session'a birden fazla key/value çifti set edilmiş bir ControllerContext hazırlar.</summary>
    public static ControllerContext BuildControllerContext(Dictionary<string, string> sessionValues)
    {
        var session = new MockSession();
        foreach (var kv in sessionValues)
            session.SetString(kv.Key, kv.Value);

        var httpContext = new DefaultHttpContext
        {
            Session = session
        };

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    /// <summary>IUrlHelper mock'u — Url.Action(...) çağrılarının null dönmesi yerine sabit string döndürmesi için.</summary>
    public static IUrlHelper BuildUrlHelper(string? returnValue = "/Dashboard/Index")
    {
        var mock = new Mock<IUrlHelper>();
        mock.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns(returnValue);
        return mock.Object;
    }
}
