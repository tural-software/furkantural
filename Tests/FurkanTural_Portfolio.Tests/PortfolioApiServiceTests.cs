using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using FurkanTural_Portfolio.Models;
using FurkanTural_Portfolio.Models.Wrappers;
using FurkanTural_Portfolio.Services;

namespace FurkanTural_Portfolio.Tests;

/// <summary>Unit tests for PortfolioApiService.<para>Each test method covers one public API (GetSkillsAsync, GetProjectsAsync, etc.). Scenarios: successful deserialisation, empty-data response, HTTP error, and network exception.</para></summary>
public class PortfolioApiServiceTests
{
    // -----------------------------------------------------------------------
    // Infrastructure helpers
    // -----------------------------------------------------------------------

    private static readonly JsonSerializerOptions SerializeOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string Wrap<T>(T data) =>
        JsonSerializer.Serialize(new ApiResult<T> { Success = true, Data = data }, SerializeOpts);

    private static (PortfolioApiService sut, Mock<HttpMessageHandler> handler) CreateSut()
    {
        var handler = new Mock<HttpMessageHandler>();
        var client = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://fake-api.local")
        };
        var logger = Mock.Of<ILogger<PortfolioApiService>>();
        return (new PortfolioApiService(client, logger), handler);
    }

    private static void SetupResponse(Mock<HttpMessageHandler> handler, string json,
        HttpStatusCode code = HttpStatusCode.OK)
    {
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(code)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }

    private static void SetupException(Mock<HttpMessageHandler> handler, Exception ex)
    {
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(ex);
    }

    // -----------------------------------------------------------------------
    // GetSkillsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSkillsAsync_WhenApiReturnsSkills_ReturnsMappedList()
    {
        // Arrange
        var skills = new[] { new SkillViewModel { Id = 1, Name = "C#", Proficiency = 90 } };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, Wrap(skills));

        // Act
        var result = await sut.GetSkillsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("C#");
        result[0].Proficiency.Should().Be(90);
    }

    [Fact]
    public async Task GetSkillsAsync_WhenApiReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupResponse(handler, Wrap(Array.Empty<SkillViewModel>()));

        // Act
        var result = await sut.GetSkillsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkillsAsync_WhenApiReturnsError_ReturnsEmptyList()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupResponse(handler, string.Empty, HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.GetSkillsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkillsAsync_WhenHttpClientThrows_ReturnsEmptyListWithoutPropagating()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupException(handler, new HttpRequestException("network error"));

        // Act
        Func<Task> act = () => sut.GetSkillsAsync();

        // Assert
        await act.Should().NotThrowAsync();
        var result = await sut.GetSkillsAsync();
        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetProjectsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProjectsAsync_WhenApiReturnsProjects_ReturnsMappedList()
    {
        // Arrange
        var projects = new[]
        {
            new ProjectViewModel { Id = 7, Title = "Blog Engine", IsCompleted = true }
        };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, Wrap(projects));

        // Act
        var result = await sut.GetProjectsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Blog Engine");
        result[0].IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetProjectsAsync_WhenApiReturnsNullData_ReturnsEmptyList()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        // ApiResult<T> with Data = null
        SetupResponse(handler, JsonSerializer.Serialize(new ApiResult<IEnumerable<ProjectViewModel>>
        {
            Success = true,
            Data    = null
        }, SerializeOpts));

        // Act
        var result = await sut.GetProjectsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectsAsync_WhenNetworkException_ReturnsEmptyWithoutThrowing()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupException(handler, new TaskCanceledException("timeout"));

        // Act + Assert
        var result = await sut.GetProjectsAsync();
        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetProjectByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProjectByIdAsync_WhenApiReturnsProject_ReturnsViewModel()
    {
        // Arrange
        var project = new ProjectViewModel { Id = 42, Title = "My App", GitHubUrl = "https://github.com/x/y" };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, JsonSerializer.Serialize(
            new ApiResult<ProjectViewModel> { Success = true, Data = project }, SerializeOpts));

        // Act
        var result = await sut.GetProjectByIdAsync(42);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.GitHubUrl.Should().Be("https://github.com/x/y");
    }

    [Fact]
    public async Task GetProjectByIdAsync_WhenApiReturnsNullData_ReturnsNull()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupResponse(handler, JsonSerializer.Serialize(
            new ApiResult<ProjectViewModel> { Success = true, Data = null }, SerializeOpts));

        // Act
        var result = await sut.GetProjectByIdAsync(99);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProjectByIdAsync_WhenHttpClientThrows_ReturnsNull()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupException(handler, new HttpRequestException("API down"));

        // Act + Assert
        var result = await sut.GetProjectByIdAsync(1);
        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // GetProjectImagesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProjectImagesAsync_WhenApiReturnsImages_ReturnsList()
    {
        // Arrange
        var images = new[]
        {
            new RemoteImageViewModel { Url = "projects/img/1.jpg", AltText = "Cover", IsCover = true }
        };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, Wrap(images));

        // Act
        var result = await sut.GetProjectImagesAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsCover.Should().BeTrue();
    }

    [Fact]
    public async Task GetProjectImagesAsync_WhenApiReturnsError_ReturnsEmptyList()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupResponse(handler, string.Empty, HttpStatusCode.NotFound);

        // Act
        var result = await sut.GetProjectImagesAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetSongsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSongsAsync_WhenApiReturnsSongs_ReturnsMappedList()
    {
        // Arrange
        var songs = new[]
        {
            new SongViewModel { Id = 3, Name = "Test Song", Artist = "Test Artist" }
        };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, Wrap(songs));

        // Act
        var result = await sut.GetSongsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Test Song");
        result[0].Artist.Should().Be("Test Artist");
    }

    [Fact]
    public async Task GetSongsAsync_WhenHttpClientThrows_ReturnsEmptyList()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupException(handler, new HttpRequestException("DNS failure"));

        // Act
        var result = await sut.GetSongsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetSongByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSongByIdAsync_WhenApiReturnsSong_ReturnsViewModel()
    {
        // Arrange
        var song = new SongViewModel { Id = 5, Name = "Anthem", Genre = "Rock" };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, JsonSerializer.Serialize(
            new ApiResult<SongViewModel> { Success = true, Data = song }, SerializeOpts));

        // Act
        var result = await sut.GetSongByIdAsync(5);

        // Assert
        result.Should().NotBeNull();
        result!.Genre.Should().Be("Rock");
    }

    [Fact]
    public async Task GetSongByIdAsync_WhenApiReturnsNullData_ReturnsNull()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupResponse(handler, JsonSerializer.Serialize(
            new ApiResult<SongViewModel> { Success = true, Data = null }, SerializeOpts));

        // Act
        var result = await sut.GetSongByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // GetExperiencesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetExperiencesAsync_WhenApiReturnsExperiences_ReturnsMappedList()
    {
        // Arrange
        var exps = new[]
        {
            new ExperienceViewModel
            {
                Id = 1, Position = "Backend Dev",
                CompanyName = "Acme Corp",
                StartDate = new DateTime(2020, 1, 1),
                EndDate   = new DateTime(2022, 6, 30)
            }
        };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, Wrap(exps));

        // Act
        var result = await sut.GetExperiencesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetExperiencesAsync_WhenApiReturnsError_ReturnsEmptyList()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupResponse(handler, string.Empty, HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await sut.GetExperiencesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetEducationsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetEducationsAsync_WhenApiReturnsEducations_ReturnsMappedList()
    {
        // Arrange
        var edus = new[]
        {
            new EducationViewModel
            {
                Id = 2, Institution = "State University",
                Degree = "B.Sc.", FieldOfStudy = "Computer Science"
            }
        };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, Wrap(edus));

        // Act
        var result = await sut.GetEducationsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Degree.Should().Be("B.Sc.");
    }

    [Fact]
    public async Task GetEducationsAsync_WhenHttpClientThrows_ReturnsEmptyList()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        SetupException(handler, new TaskCanceledException("operation timeout"));

        // Act
        var result = await sut.GetEducationsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetSongImagesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSongImagesAsync_WhenApiReturnsImages_ReturnsList()
    {
        // Arrange
        var images = new[]
        {
            new RemoteImageViewModel { Url = "music/img/cover.jpg", IsCover = true }
        };
        var (sut, handler) = CreateSut();
        SetupResponse(handler, Wrap(images));

        // Act
        var result = await sut.GetSongImagesAsync(5);

        // Assert
        result.Should().HaveCount(1);
        result[0].Url.Should().Be("music/img/cover.jpg");
    }
}
