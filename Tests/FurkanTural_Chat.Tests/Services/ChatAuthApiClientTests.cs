using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using FurkanTural_Chat.Models.Auth;
using FurkanTural_Chat.Models.Wrappers;
using FurkanTural_Chat.Services;

namespace FurkanTural_Chat.Tests.Services;

/// <summary>
/// ChatAuthApiClient unit testleri.
/// HttpMessageHandler Moq ile stub'lanir; gercek HTTP istegi gitmez.
/// </summary>
public class ChatAuthApiClientTests
{
    // ---- Fabrika yardimcilari ----

    private static (ChatAuthApiClient sut, Mock<HttpMessageHandler> handler) CreateSut(
        HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.test.local") };
        return (new ChatAuthApiClient(http, NullLogger<ChatAuthApiClient>.Instance), handler);
    }

    private static (ChatAuthApiClient sut, Mock<HttpMessageHandler> handler) CreateThrowingSut(
        Exception ex)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(ex);

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.test.local") };
        return (new ChatAuthApiClient(http, NullLogger<ChatAuthApiClient>.Instance), handler);
    }

    private static HttpResponseMessage OkJson<T>(T payload, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    // ---- LoginAsync: basarili senaryo ----

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var authData = new AuthResultModel { Token = "tok-123", UserId = 42, Username = "testuser" };
        var apiResp = new ApiResult<AuthResultModel> { Success = true, StatusCode = 200, Data = authData };
        var (sut, _) = CreateSut(OkJson(apiResp));

        // Act
        var result = await sut.LoginAsync(new LoginRequestModel { Username = "testuser", Password = "P@ssw0rd" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("tok-123");
        result.Data.UserId.Should().Be(42);
        result.Data.Username.Should().Be("testuser");
    }

    // ---- LoginAsync: API 401 ----

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsApiFailResult()
    {
        // Arrange
        var apiResp = new ApiResult<AuthResultModel>
        {
            Success = false, StatusCode = 401, Message = "Hatali kimlik.", Data = null
        };
        var (sut, _) = CreateSut(OkJson(apiResp, HttpStatusCode.Unauthorized));

        // Act
        var result = await sut.LoginAsync(new LoginRequestModel { Username = "u", Password = "wrong" });

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Hatali kimlik.");
    }

    // ---- LoginAsync: HttpRequestException -> 503 ----

    [Fact]
    public async Task LoginAsync_ApiDown_Returns503WithConnectionError()
    {
        // Arrange
        var (sut, _) = CreateThrowingSut(new HttpRequestException("Connection refused"));

        // Act
        var result = await sut.LoginAsync(new LoginRequestModel { Username = "u", Password = "p" });

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(503);
        result.Errors.Should().ContainSingle().Which.Should().Contain("eri");
    }

    // ---- LoginAsync: TaskCanceledException -> 504 ----

    [Fact]
    public async Task LoginAsync_RequestTimesOut_Returns504()
    {
        // Arrange
        var (sut, _) = CreateThrowingSut(new TaskCanceledException("Timeout"));

        // Act
        var result = await sut.LoginAsync(new LoginRequestModel { Username = "u", Password = "p" });

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(504);
        result.Errors.Should().ContainSingle().Which.Should().Contain("zaman");
    }

    // ---- LoginAsync: null JSON (bos yanit) ----

    [Fact]
    public async Task LoginAsync_NullJsonBody_ReturnsFailWithEmptyResponseMessage()
    {
        // Arrange
        var emptyResp = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };
        var (sut, _) = CreateSut(emptyResp);

        // Act
        var result = await sut.LoginAsync(new LoginRequestModel { Username = "u", Password = "p" });

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("bo");
    }

    // ---- LoginAsync: beklenmeyen Exception -> 500 ----

    [Fact]
    public async Task LoginAsync_UnexpectedException_Returns500()
    {
        // Arrange
        var (sut, _) = CreateThrowingSut(new InvalidOperationException("Beklenmeyen hata"));

        // Act
        var result = await sut.LoginAsync(new LoginRequestModel { Username = "u", Password = "p" });

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.Errors.Should().ContainSingle().Which.Should().Contain("Beklenmeyen");
    }

    // ---- LoginAsync: request body'de appSource=Chat kontrol ----

    [Fact]
    public async Task LoginAsync_Always_SendsAppSourceAsChatInRequestBody()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage r, CancellationToken _) =>
            {
                capturedBody = await r.Content!.ReadAsStringAsync();
                var ok = new ApiResult<AuthResultModel> { Success = true, Data = new AuthResultModel { Token = "t" } };
                return OkJson(ok);
            });

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.test.local") };
        var sut = new ChatAuthApiClient(http, NullLogger<ChatAuthApiClient>.Instance);

        // Act
        await sut.LoginAsync(new LoginRequestModel { Username = "u", Password = "p" });

        // Assert
        capturedBody.Should().Contain("appSource");
        capturedBody.Should().Contain("Chat");
    }

    // ---- RegisterAsync: basarili senaryo ----

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccessWithToken()
    {
        // Arrange
        var authData = new AuthResultModel { Token = "reg-tok", UserId = 99, Username = "newuser" };
        var apiResp = new ApiResult<AuthResultModel> { Success = true, Data = authData };
        var (sut, _) = CreateSut(OkJson(apiResp));
        var req = new RegisterRequestModel
        {
            Username = "newuser", Email = "new@ex.com", Password = "P@ss1", AcceptAgreement = true
        };

        // Act
        var result = await sut.RegisterAsync(req);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Token.Should().Be("reg-tok");
        result.Data.UserId.Should().Be(99);
    }

    // ---- RegisterAsync: API 409 (kullanici adi mevcut) ----

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ReturnsApiError()
    {
        // Arrange
        var apiResp = new ApiResult<AuthResultModel>
        {
            Success = false,
            StatusCode = 409,
            Errors = new List<string> { "Kullanici adi zaten alinmis." },
            Data = null
        };
        var (sut, _) = CreateSut(OkJson(apiResp, HttpStatusCode.Conflict));
        var req = new RegisterRequestModel { Username = "existing", Email = "e@e.com", Password = "P@ss", AcceptAgreement = true };

        // Act
        var result = await sut.RegisterAsync(req);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    // ---- RegisterAsync: HttpRequestException -> 503 ----

    [Fact]
    public async Task RegisterAsync_ApiDown_Returns503()
    {
        // Arrange
        var (sut, _) = CreateThrowingSut(new HttpRequestException("down"));
        var req = new RegisterRequestModel { Username = "u", Email = "e@e.com", Password = "p", AcceptAgreement = true };

        // Act
        var result = await sut.RegisterAsync(req);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(503);
    }

    // ---- RegisterAsync: zaman asimi -> 504 ----

    [Fact]
    public async Task RegisterAsync_Timeout_Returns504()
    {
        // Arrange
        var (sut, _) = CreateThrowingSut(new TaskCanceledException("timeout"));
        var req = new RegisterRequestModel { Username = "u", Email = "e@e.com", Password = "p", AcceptAgreement = true };

        // Act
        var result = await sut.RegisterAsync(req);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(504);
    }

    // ---- RegisterAsync: null JSON (bos yanit) ----

    [Fact]
    public async Task RegisterAsync_NullJsonBody_ReturnsFailWithEmptyResponseMessage()
    {
        // Arrange
        var emptyResp = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };
        var (sut, _) = CreateSut(emptyResp);
        var req = new RegisterRequestModel { Username = "u", Email = "e@e.com", Password = "p", AcceptAgreement = true };

        // Act
        var result = await sut.RegisterAsync(req);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("bo");
    }
}
