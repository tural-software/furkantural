using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FurkanTural_Admin.Models.Auth;
using FurkanTural_Admin.Models.Wrappers;
using FurkanTural_Admin.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace FurkanTural_Admin.Tests.Services;

public class AuthApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static AuthApiClient BuildSut(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.test/")
        };
        return new AuthApiClient(httpClient, NullLogger<AuthApiClient>.Instance);
    }

    private static Mock<HttpMessageHandler> BuildHandlerMock(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return handlerMock;
    }

    // ── Başarılı login ───────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_SuccessResponse_ReturnsSuccessResult()
    {
        // Arrange
        var apiResult = new ApiResult<LoginResultModel>
        {
            Success = true,
            Data = new LoginResultModel
            {
                Token     = "jwt-token",
                Username  = "furkan",
                RoleName  = "Admin",
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(apiResult, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        var sut = BuildSut(BuildHandlerMock(response).Object);
        var request = new LoginRequestModel { Username = "furkan", Password = "pass" };

        // Act
        var result = await sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("jwt-token");
        result.Data.RoleName.Should().Be("Admin");
    }

    // ── 500 Internal Server Error ────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ApiReturns500_ReturnsFailResult()
    {
        // Arrange
        // API 500 döndüğünde, response JSON okunabilir ama Success=false olur
        var apiResult = new ApiResult<LoginResultModel>
        {
            Success    = false,
            Message    = "İç sunucu hatası.",
            StatusCode = 500
        };

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(apiResult, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        var sut     = BuildSut(BuildHandlerMock(response).Object);
        var request = new LoginRequestModel { Username = "furkan", Password = "pass" };

        // Act
        var result = await sut.LoginAsync(request, CancellationToken.None);

        // Assert: ya null content'ten Fail, ya da API'nin false döndürdüğü result
        result.Should().NotBeNull();
        // Başarılı OLMAMALI
        result.Success.Should().BeFalse();
    }

    // ── HttpRequestException (sunucu ulaşılamaz) ─────────────────────────────

    [Fact]
    public async Task LoginAsync_HttpRequestException_ReturnsFailWithGracefulMessage()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Sunucuya bağlanılamadı."));

        var sut     = BuildSut(handlerMock.Object);
        var request = new LoginRequestModel { Username = "furkan", Password = "pass" };

        // Act
        var result = await sut.LoginAsync(request, CancellationToken.None);

        // Assert: exception yakalanmış, graceful Fail dönmeli
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.First().Should().Contain("erişilemedi");
    }

    // ── TaskCanceledException (timeout) ─────────────────────────────────────

    [Fact]
    public async Task LoginAsync_TaskCanceled_ReturnsFailWithTimeoutMessage()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out."));

        var sut     = BuildSut(handlerMock.Object);
        var request = new LoginRequestModel { Username = "furkan", Password = "pass" };

        // Act
        var result = await sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.First().Should().Contain("zaman aşımı");
    }

    // ── Boş/null response body ────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_EmptyResponseBody_ReturnsFailResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };

        var sut     = BuildSut(BuildHandlerMock(response).Object);
        var request = new LoginRequestModel { Username = "furkan", Password = "pass" };

        // Act
        var result = await sut.LoginAsync(request, CancellationToken.None);

        // Assert: null body => Fail
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }
}
