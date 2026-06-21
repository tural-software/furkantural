using System.Net;
using System.Text.Json;
using FluentAssertions;
using FurkanTural_Admin.Models.Blog;
using FurkanTural_Admin.Models.Wrappers;
using FurkanTural_Admin.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace FurkanTural_Admin.Tests.Services;

public class BlogApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static BlogApiClient BuildSut(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.test/")
        };
        return new BlogApiClient(httpClient, NullLogger<BlogApiClient>.Instance);
    }

    private static Mock<HttpMessageHandler> BuildHandlerMock(HttpResponseMessage response)
    {
        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return mock;
    }

    private static Mock<HttpMessageHandler> BuildThrowingHandlerMock(Exception ex)
    {
        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(ex);
        return mock;
    }

    [Fact]
    public async Task GetAllForAdminAsync_SuccessResponse_ReturnsBlogList()
    {
        var blogs = new List<BlogAdminDto>
        {
            new() { Id = 1, Title = "Test Blog", IsActive = true },
            new() { Id = 2, Title = "Pasif Blog", IsActive = false }
        };
        var wrapper = new ApiResult<IEnumerable<BlogAdminDto>> { Success = true, Data = blogs };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(wrapper, JsonOptions),
                System.Text.Encoding.UTF8, "application/json")
        };
        var sut = BuildSut(BuildHandlerMock(response).Object);

        var result = await sut.GetAllForAdminAsync("jwt-token", CancellationToken.None);

        result.Should().HaveCount(2);
        result.First().Title.Should().Be("Test Blog");
    }

    [Fact]
    public async Task GetAllForAdminAsync_ApiReturns401_ReturnsEmptyList()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        var sut = BuildSut(BuildHandlerMock(response).Object);

        var result = await sut.GetAllForAdminAsync("expired-token", CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllForAdminAsync_HttpRequestException_ReturnsEmptyList()
    {
        var sut = BuildSut(BuildThrowingHandlerMock(new HttpRequestException("Network error")).Object);

        var result = await sut.GetAllForAdminAsync("jwt-token", CancellationToken.None);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetAllForAdminAsync_TaskCanceled_ReturnsEmptyList()
    {
        var sut = BuildSut(BuildThrowingHandlerMock(new TaskCanceledException("Timeout")).Object);

        var result = await sut.GetAllForAdminAsync("jwt-token", CancellationToken.None);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_SuccessResponse_ReturnsTrue()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var sut = BuildSut(BuildHandlerMock(response).Object);

        var result = await sut.DeleteAsync(1, "jwt-token", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ApiReturns500_ReturnsFalse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var sut = BuildSut(BuildHandlerMock(response).Object);

        var result = await sut.DeleteAsync(1, "jwt-token", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_HttpRequestException_ReturnsFalse()
    {
        var sut = BuildSut(BuildThrowingHandlerMock(new HttpRequestException("Network")).Object);

        var result = await sut.DeleteAsync(1, "jwt-token", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActiveAsync_SuccessResponse_ReturnsTrue()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var sut = BuildSut(BuildHandlerMock(response).Object);

        var result = await sut.ToggleActiveAsync(3, "jwt-token", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActiveAsync_TaskCanceled_ReturnsFalse()
    {
        var sut = BuildSut(BuildThrowingHandlerMock(new TaskCanceledException()).Object);

        var result = await sut.ToggleActiveAsync(3, "jwt-token", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_SuccessResponse_ReturnsTrue()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var sut = BuildSut(BuildHandlerMock(response).Object);
        var dto = new BlogFormDto { Title = "Yeni Blog", Content = "Icerik" };

        var result = await sut.CreateAsync(dto, "jwt-token", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ApiReturns400_ReturnsFalse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var sut = BuildSut(BuildHandlerMock(response).Object);
        var dto = new BlogFormDto { Title = null, Content = null };

        var result = await sut.CreateAsync(dto, "jwt-token", CancellationToken.None);

        result.Should().BeFalse();
    }
}
