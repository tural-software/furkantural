using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using FurkanTural_Blog.Models;
using FurkanTural_Blog.Models.Wrappers;
using FurkanTural_Blog.Services;

namespace FurkanTural_Blog.Tests;

/// <summary>
/// BlogApiService testleri — HttpMessageHandler mock'u ile HttpClient'i ele geçirir.
/// Bağımlılık: yalnızca HttpClient (handler mock) + NullLogger. Ağ/DB gerektirmez.
/// </summary>
public class BlogApiServiceTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (BlogApiService service, Mock<HttpMessageHandler> handlerMock) BuildService(
        HttpStatusCode statusCode,
        object? responseBody)
    {
        var json = responseBody is null ? "{}" : JsonSerializer.Serialize(responseBody);

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.test/")
        };

        var logger = NullLogger<BlogApiService>.Instance;
        var service = new BlogApiService(client, logger);
        return (service, handlerMock);
    }

    private static (BlogApiService service, Mock<HttpMessageHandler> handlerMock) BuildServiceThrows(
        Exception exception)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.test/")
        };

        var logger = NullLogger<BlogApiService>.Instance;
        var service = new BlogApiService(client, logger);
        return (service, handlerMock);
    }

    // ── GetPostAsync — success ────────────────────────────────────────────────

    [Fact]
    public async Task GetPostAsync_WhenApiReturnsPost_ReturnsPopulatedViewModel()
    {
        // Arrange
        var expectedPost = new BlogPostViewModel { Id = 42, Title = "Test Post", Content = "Icerik" };
        var responseBody = new ApiResult<BlogPostViewModel> { Success = true, Data = expectedPost };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetPostAsync(42);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Title.Should().Be("Test Post");
    }

    [Fact]
    public async Task GetPostAsync_WhenApiReturnsNullData_ReturnsNull()
    {
        // Arrange
        var responseBody = new ApiResult<BlogPostViewModel> { Success = true, Data = null };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetPostAsync(99);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPostAsync_WhenApiThrows_ReturnsNull()
    {
        // Arrange
        var (service, _) = BuildServiceThrows(new HttpRequestException("Connection refused"));

        // Act
        var result = await service.GetPostAsync(1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPostAsync_WhenApiReturns404_ReturnsNull()
    {
        // Arrange — 404 causes GetFromJsonAsync to throw HttpRequestException
        var (service, _) = BuildService(HttpStatusCode.NotFound, null);

        // Act
        var result = await service.GetPostAsync(999);

        // Assert — service catches and returns null
        result.Should().BeNull();
    }

    // ── GetPostsAsync — success ───────────────────────────────────────────────

    [Fact]
    public async Task GetPostsAsync_WhenApiReturnsPosts_ReturnsNonEmptyList()
    {
        // Arrange
        var posts = new List<BlogPostViewModel>
        {
            new() { Id = 1, Title = "Yazi 1" },
            new() { Id = 2, Title = "Yazi 2" }
        };
        var responseBody = new ApiResult<IEnumerable<BlogPostViewModel>> { Success = true, Data = posts };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetPostsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Yazi 1");
    }

    [Fact]
    public async Task GetPostsAsync_WhenApiReturnsEmptyList_ReturnsEmptyCollection()
    {
        // Arrange
        var responseBody = new ApiResult<IEnumerable<BlogPostViewModel>>
        {
            Success = true,
            Data = Enumerable.Empty<BlogPostViewModel>()
        };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetPostsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPostsAsync_WhenApiThrows_ReturnsEmptyCollection()
    {
        // Arrange
        var (service, _) = BuildServiceThrows(new TaskCanceledException("Timeout"));

        // Act
        var result = await service.GetPostsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPostsAsync_WhenApiReturnsNullData_ReturnsEmptyCollection()
    {
        // Arrange
        var responseBody = new ApiResult<IEnumerable<BlogPostViewModel>> { Success = true, Data = null };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetPostsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── GetCategoriesAsync — success ──────────────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_WhenApiReturnsCategories_ReturnsSortedByName()
    {
        // Arrange
        var categories = new List<CategoryViewModel>
        {
            new() { Id = 1, Name = "Zebra" },
            new() { Id = 2, Name = "Alpha" },
            new() { Id = 3, Name = "Mango" }
        };
        var responseBody = new ApiResult<IEnumerable<CategoryViewModel>> { Success = true, Data = categories };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetCategoriesAsync();

        // Assert — service sorts alphabetically
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Mango");
        result[2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetCategoriesAsync_WhenSomeCategoriesHaveNullName_FiltersThemOut()
    {
        // Arrange
        var categories = new List<CategoryViewModel>
        {
            new() { Id = 1, Name = "Gecerli" },
            new() { Id = 2, Name = null },          // should be filtered
            new() { Id = 3, Name = "   " }           // whitespace — should be filtered
        };
        var responseBody = new ApiResult<IEnumerable<CategoryViewModel>> { Success = true, Data = categories };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetCategoriesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Gecerli");
    }

    [Fact]
    public async Task GetCategoriesAsync_WhenApiThrows_ReturnsEmptyCollection()
    {
        // Arrange
        var (service, _) = BuildServiceThrows(new HttpRequestException("Network error"));

        // Act
        var result = await service.GetCategoriesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── GetSitemapItemsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetSitemapItemsAsync_WhenApiReturnsItems_ReturnsAll()
    {
        // Arrange
        var items = new List<BlogSitemapItem>
        {
            new() { Id = 1, CreatedAt = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 2, 2) },
            new() { Id = 2, CreatedAt = new DateTime(2026, 3, 3), UpdatedAt = null }
        };
        var responseBody = new ApiResult<IEnumerable<BlogSitemapItem>> { Success = true, Data = items };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetSitemapItemsAsync();

        // Assert
        result.Should().HaveCount(2);
        // LastModified, UpdatedAt varsa onu, yoksa CreatedAt'i kullanır.
        result[0].LastModified.Should().Be(new DateTime(2026, 2, 2));
        result[1].LastModified.Should().Be(new DateTime(2026, 3, 3));
    }

    [Fact]
    public async Task GetSitemapItemsAsync_WhenApiThrows_ReturnsEmptyCollection()
    {
        // Arrange
        var (service, _) = BuildServiceThrows(new HttpRequestException("Unreachable"));

        // Act
        var result = await service.GetSitemapItemsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── GetImagesByBlogAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetImagesByBlogAsync_WhenApiReturnsImages_ReturnsImagesForBlog()
    {
        // Arrange
        var images = new List<BlogImageViewModel>
        {
            new() { Url = "blog/images/post5.jpg", IsCover = true, BlogId = 5 }
        };
        var responseBody = new ApiResult<IEnumerable<BlogImageViewModel>> { Success = true, Data = images };
        var (service, _) = BuildService(HttpStatusCode.OK, responseBody);

        // Act
        var result = await service.GetImagesByBlogAsync(5);

        // Assert
        result.Should().HaveCount(1);
        result[0].BlogId.Should().Be(5);
    }

    [Fact]
    public async Task GetImagesByBlogAsync_WhenApiThrows_ReturnsEmptyCollection()
    {
        // Arrange
        var (service, _) = BuildServiceThrows(new HttpRequestException("Timeout"));

        // Act
        var result = await service.GetImagesByBlogAsync(5);

        // Assert
        result.Should().BeEmpty();
    }

    // ── GetPostsPagedAsync — success ──────────────────────────────────────────

    [Fact]
    public async Task GetPostsPagedAsync_WhenApiReturnsPagedData_ReturnsMappedViewModel()
    {
        // Arrange
        var posts = new List<BlogPostViewModel>
        {
            new() { Id = 1, Title = "Post 1" },
            new() { Id = 2, Title = "Post 2" }
        };

        // GetPostsPagedAsync calls GetCategoriesAsync first (same handler).
        // We need a handler that returns categories on first call and paged posts on second.
        var categoriesBody = new ApiResult<IEnumerable<CategoryViewModel>>
        {
            Success = true,
            Data = new List<CategoryViewModel> { new() { Id = 1, Name = "Dotnet" } }
        };
        var pagedBody = new PagedApiResult<IEnumerable<BlogPostViewModel>>
        {
            Success = true,
            Data = posts,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call: GetCategoriesAsync, second call: GetPostsPagedAsync
                var body = callCount == 1
                    ? JsonSerializer.Serialize(categoriesBody)
                    : JsonSerializer.Serialize(pagedBody);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            });

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.test/") };
        var service = new BlogApiService(client, NullLogger<BlogApiService>.Instance);

        // Act
        var result = await service.GetPostsPagedAsync(1, 10, null, null);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetPostsPagedAsync_WhenApiThrowsOnPagedEndpoint_ReturnsEmptyPagedViewModel()
    {
        // Arrange — first call (categories) OK, second (paged posts) throws
        var categoriesBody = new ApiResult<IEnumerable<CategoryViewModel>>
        {
            Success = true,
            Data = Enumerable.Empty<CategoryViewModel>()
        };

        var callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            JsonSerializer.Serialize(categoriesBody), Encoding.UTF8, "application/json")
                    };
                }
                // Second call throws
                throw new HttpRequestException("API is down");
            });

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.test/") };
        var service = new BlogApiService(client, NullLogger<BlogApiService>.Instance);

        // Act
        var result = await service.GetPostsPagedAsync(1, 10, null, null);

        // Assert — fallback empty result
        result.Items.Should().BeEmpty();
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPostsPagedAsync_WhenCategoryFilterSet_PassesCategoryIdInUrl()
    {
        // Arrange
        var capturedRequest = new List<HttpRequestMessage>();
        var categoriesBody = new ApiResult<IEnumerable<CategoryViewModel>>
        {
            Success = true,
            Data = new List<CategoryViewModel> { new() { Id = 5, Name = "DevOps" } }
        };
        var pagedBody = new PagedApiResult<IEnumerable<BlogPostViewModel>>
        {
            Success = true,
            Data = Enumerable.Empty<BlogPostViewModel>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        var callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest.Add(req))
            .ReturnsAsync(() =>
            {
                callCount++;
                var body = callCount == 1
                    ? JsonSerializer.Serialize(categoriesBody)
                    : JsonSerializer.Serialize(pagedBody);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            });

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.test/") };
        var service = new BlogApiService(client, NullLogger<BlogApiService>.Instance);

        // Act
        await service.GetPostsPagedAsync(1, 10, categoryId: 5, search: null);

        // Assert — the second request should contain categoryId=5
        capturedRequest.Should().HaveCount(2);
        var pagedUrl = capturedRequest[1].RequestUri!.ToString();
        pagedUrl.Should().Contain("categoryId=5");
    }

    [Fact]
    public async Task GetPostsPagedAsync_WhenSearchSet_PassesEncodedSearchInUrl()
    {
        // Arrange
        var capturedRequest = new List<HttpRequestMessage>();
        var categoriesBody = new ApiResult<IEnumerable<CategoryViewModel>>
        {
            Success = true,
            Data = Enumerable.Empty<CategoryViewModel>()
        };
        var pagedBody = new PagedApiResult<IEnumerable<BlogPostViewModel>>
        {
            Success = true,
            Data = Enumerable.Empty<BlogPostViewModel>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        var callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest.Add(req))
            .ReturnsAsync(() =>
            {
                callCount++;
                var body = callCount == 1
                    ? JsonSerializer.Serialize(categoriesBody)
                    : JsonSerializer.Serialize(pagedBody);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            });

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.test/") };
        var service = new BlogApiService(client, NullLogger<BlogApiService>.Instance);

        // Act
        await service.GetPostsPagedAsync(1, 10, categoryId: null, search: "dotnet core");

        // Assert — URL should contain encoded search term
        capturedRequest.Should().HaveCount(2);
        var pagedUrl = capturedRequest[1].RequestUri!.ToString();
        pagedUrl.Should().Contain("search=");
        pagedUrl.Should().Contain("dotnet");
    }
}
