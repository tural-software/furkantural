using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FurkanTural_Portfolio.Tests;

/// <summary>
/// Unit tests for AppTokenService.
///
/// Strategy: AppTokenService constructor takes IHttpClientFactory.  We supply a real
/// HttpClient backed by a mocked HttpMessageHandler so the JSON deserialisation pipeline
/// runs end-to-end without exposing the private nested AppTokenResponse/TokenData types.
/// </summary>
public class AppTokenServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Produces the JSON shape the production code expects to deserialise.
    /// Matches private nested class AppTokenResponse { TokenData Data }:
    ///   { "data": { "token": "...", "expiresAt": "..." } }
    /// </summary>
    private static string BuildTokenJson(string token, DateTime expiresAt) =>
        JsonSerializer.Serialize(new
        {
            data = new { token, expiresAt = expiresAt.ToString("o") }
        });

    private static (AppTokenService sut, Mock<HttpMessageHandler> handlerMock) CreateSut(
        Mock<HttpMessageHandler> handlerMock,
        string appKey = "key",
        string appName = "app")
    {
        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://fake-api.local")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("AppTokenClient")).Returns(client);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:AppKey"]  = appKey,
                ["Api:AppName"] = appName
            })
            .Build();

        var logger = Mock.Of<ILogger<AppTokenService>>();
        return (new AppTokenService(factory.Object, config, logger), handlerMock);
    }

    // -----------------------------------------------------------------------
    // Success path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTokenAsync_WhenApiReturnsValidToken_ReturnsToken()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    BuildTokenJson("access-token-001", DateTime.UtcNow.AddHours(2)),
                    Encoding.UTF8, "application/json")
            });

        var (sut, _) = CreateSut(handler);

        // Act
        var result = await sut.GetTokenAsync();

        // Assert
        result.Should().Be("access-token-001");
    }

    // -----------------------------------------------------------------------
    // Double-checked locking / caching
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTokenAsync_WhenCalledTwiceWithValidCache_CallsApiOnlyOnce()
    {
        // Arrange - expiry far in the future (3 h) so the -1 h adjustment still keeps it live
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    BuildTokenJson("cached-token", DateTime.UtcNow.AddHours(3)),
                    Encoding.UTF8, "application/json")
            });

        var (sut, _) = CreateSut(handler);

        // Act
        var first  = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        // Assert
        first.Should().Be("cached-token");
        second.Should().Be("cached-token");
        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Token expiry: re-fetch when stored expiry has passed
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTokenAsync_WhenStoredTokenExpired_FetchesNewToken()
    {
        // Arrange
        // Call 1: ExpiresAt = UtcNow + 59 min  →  _tokenExpiry = UtcNow - 1 min  (already past)
        // Call 2: must not hit cache, fetches fresh token
        var handler = new Mock<HttpMessageHandler>();
        int calls = 0;
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                calls++;
                var (tkn, exp) = calls == 1
                    ? ("old-token", DateTime.UtcNow.AddMinutes(59))   // triggers re-fetch
                    : ("new-token", DateTime.UtcNow.AddHours(3));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildTokenJson(tkn, exp), Encoding.UTF8, "application/json")
                };
            });

        var (sut, _) = CreateSut(handler);

        // Act
        var first  = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        // Assert
        first.Should().Be("old-token");
        second.Should().Be("new-token");
        handler.Protected().Verify("SendAsync", Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Failure backoff (negative cache)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTokenAsync_WhenApiReturnsErrorStatus_ReturnsEmptyString()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var (sut, _) = CreateSut(handler);

        // Act
        var result = await sut.GetTokenAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTokenAsync_DuringBackoffWindow_DoesNotCallApiAgain()
    {
        // Arrange - first call fails and sets _retryBlockedUntil = UtcNow + 15 s
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var (sut, _) = CreateSut(handler);

        // Act - second call must be blocked by the 15-second backoff window
        await sut.GetTokenAsync();
        await sut.GetTokenAsync();

        // Assert - HTTP endpoint called exactly once
        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTokenAsync_WhenHttpClientThrows_ReturnsEmptyWithoutPropagating()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var (sut, _) = CreateSut(handler);

        // Act + Assert - exception must NOT propagate to the caller
        Func<Task> act = () => sut.GetTokenAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetTokenAsync_WhenExceptionThrown_SetsBackoffSoSecondCallSkipsApi()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timeout"));

        var (sut, _) = CreateSut(handler);

        // Act
        await sut.GetTokenAsync();  // exception path → sets backoff
        await sut.GetTokenAsync();  // should be blocked by backoff

        // Assert - only one HTTP attempt despite two calls
        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // DefaultTokenHandler
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DefaultTokenHandler_WhenTokenAvailable_SetsAuthorizationHeader()
    {
        // Arrange
        var appToken = new Mock<IAppTokenService>();
        appToken.Setup(s => s.GetTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("bearer-value");

        var inner = new Mock<HttpMessageHandler>();
        inner.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var delegating = new DefaultTokenHandler(appToken.Object) { InnerHandler = inner.Object };
        var invoker = new HttpMessageInvoker(delegating);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://fake-api.local/res");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization!.Parameter.Should().Be("bearer-value");
    }

    [Fact]
    public async Task DefaultTokenHandler_WhenTokenEmpty_DoesNotSetAuthorizationHeader()
    {
        // Arrange
        var appToken = new Mock<IAppTokenService>();
        appToken.Setup(s => s.GetTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

        var inner = new Mock<HttpMessageHandler>();
        inner.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var delegating = new DefaultTokenHandler(appToken.Object) { InnerHandler = inner.Object };
        var invoker = new HttpMessageInvoker(delegating);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://fake-api.local/res");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert - header must be absent when token is empty
        request.Headers.Authorization.Should().BeNull();
    }
}
