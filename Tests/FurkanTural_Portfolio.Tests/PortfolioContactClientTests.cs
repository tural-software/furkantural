using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using FurkanTural_Portfolio.Models;
using FurkanTural_Portfolio.Services;

namespace FurkanTural_Portfolio.Tests;

/// <summary>
/// Unit tests for PortfolioContactClient.SubmitContactAsync.
///
/// PortfolioContactClient takes a pre-built HttpClient (primary-handler pattern), so we inject
/// a real HttpClient whose inner handler is mocked - letting the JSON serialisation path run.
/// </summary>
public class PortfolioContactClientTests
{
    private static (PortfolioContactClient sut, Mock<HttpMessageHandler> handler) CreateSut()
    {
        var handler = new Mock<HttpMessageHandler>();
        var client = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://fake-api.local")
        };
        var logger = Mock.Of<ILogger<PortfolioContactClient>>();
        return (new PortfolioContactClient(client, logger), handler);
    }

    private static ContactFormModel ValidModel() => new()
    {
        Name           = "Test User",
        Email          = "test@example.com",
        Message        = "Hello from unit test",
        TurnstileToken = "cf-token-abc"
    };

    // -----------------------------------------------------------------------
    // Success
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SubmitContactAsync_WhenApiReturns200_ReturnsTrue()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await sut.SubmitContactAsync(ValidModel());

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitContactAsync_WhenApiReturns201_ReturnsTrue()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        // Act
        var result = await sut.SubmitContactAsync(ValidModel());

        // Assert
        result.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // Failure
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SubmitContactAsync_WhenApiReturns400_ReturnsFalse()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        var result = await sut.SubmitContactAsync(ValidModel());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitContactAsync_WhenApiReturns500_ReturnsFalse()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var result = await sut.SubmitContactAsync(ValidModel());

        // Assert
        result.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Timeout / network error
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SubmitContactAsync_WhenHttpClientThrows_ReturnsFalseWithoutPropagating()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("request timeout"));

        // Act
        Func<Task> act = () => sut.SubmitContactAsync(ValidModel());

        // Assert - exception must NOT propagate; method returns false
        await act.Should().NotThrowAsync();
        var result = await sut.SubmitContactAsync(ValidModel());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitContactAsync_WhenNetworkException_ReturnsFalse()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network unreachable"));

        // Act
        var result = await sut.SubmitContactAsync(ValidModel());

        // Assert
        result.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Payload correctness
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SubmitContactAsync_PostsToCorrectEndpoint()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        HttpRequestMessage? captured = null;

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                captured = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        await sut.SubmitContactAsync(ValidModel());

        // Assert
        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Post);
        captured.RequestUri!.PathAndQuery.Should().Be("/api/v1/contact/submit");
    }

    [Fact]
    public async Task SubmitContactAsync_SendsJsonContentType()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        HttpRequestMessage? captured = null;

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                captured = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        await sut.SubmitContactAsync(ValidModel());

        // Assert
        captured.Should().NotBeNull();
        captured!.Content.Should().NotBeNull();
        captured.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}
