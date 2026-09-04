using FluentAssertions;
using FurkanTural_Admin.Controllers;
using FurkanTural_Admin.Helpers;
using FurkanTural_Admin.Models.Common;
using FurkanTural_Admin.Models.Skill;
using FurkanTural_Admin.Services;
using FurkanTural_Admin.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FurkanTural_Admin.Tests.Controllers;

/// <summary>Token olmadan erişildiğinde her controller'ın yetkisiz erişimi engellediğini (redirect veya 401) doğrular.</summary>
public class SessionGuardTests
{
    // ── SkillController ──────────────────────────────────────────────────────

    [Fact]
    public async Task SkillController_Index_WithoutToken_RedirectsToLogin()
    {
        // Arrange
        var mock = new Mock<ISkillApiClient>(MockBehavior.Loose);
        var sut  = new SkillController(mock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null)
        };

        // Act
        var result = await sut.Index(null, null, null, null, null, 1, 10, CancellationToken.None);

        // Assert: Index, token yoksa Login'e redirect eder
        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Login");
        result.As<RedirectToActionResult>().ControllerName.Should().Be("Auth");
    }

    [Fact]
    public async Task SkillController_TablePartial_WithoutToken_Returns401()
    {
        // Arrange
        var mock = new Mock<ISkillApiClient>(MockBehavior.Loose);
        var sut  = new SkillController(mock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null)
        };

        // Act
        var result = await sut.TablePartial(null, null, null, null, null, 1, 10, CancellationToken.None);

        // Assert: AJAX endpoint 401 döner (redirect değil)
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task SkillController_Delete_WithoutToken_Returns401()
    {
        // Arrange
        var mock = new Mock<ISkillApiClient>(MockBehavior.Loose);
        var sut  = new SkillController(mock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null)
        };

        // Act
        var result = await sut.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task SkillController_Create_WithoutToken_Returns401()
    {
        // Arrange
        var mock = new Mock<ISkillApiClient>(MockBehavior.Loose);
        var sut  = new SkillController(mock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null)
        };

        // Act
        var result = await sut.Create(new SkillFormDto { Name = "C#", Proficiency = 90 }, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ── BlogController ───────────────────────────────────────────────────────

    [Fact]
    public async Task BlogController_Index_WithoutToken_RedirectsToLogin()
    {
        // Arrange
        var blogMock     = new Mock<IBlogApiClient>(MockBehavior.Loose);
        var categoryMock = new Mock<ICategoryApiClient>(MockBehavior.Loose);
        var sut = new BlogController(blogMock.Object, categoryMock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null)
        };

        // Act
        var result = await sut.Index(null, null, null, null, null, null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task BlogController_Delete_WithoutToken_Returns401()
    {
        // Arrange
        var blogMock     = new Mock<IBlogApiClient>(MockBehavior.Loose);
        var categoryMock = new Mock<ICategoryApiClient>(MockBehavior.Loose);
        var sut = new BlogController(blogMock.Object, categoryMock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null)
        };

        // Act
        var result = await sut.Delete(5, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ── DashboardController ──────────────────────────────────────────────────

    [Fact]
    public async Task DashboardController_Index_WithoutToken_RedirectsToLogin()
    {
        // Arrange
        var mock = new Mock<IAdminSummaryClient>(MockBehavior.Loose);
        var sut  = new DashboardController(mock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext((string?)null)
        };

        // Act
        var result = await sut.Index(CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be("Login");
        result.As<RedirectToActionResult>().ControllerName.Should().Be("Auth");
    }

    // ── SkillController — token varken Index çalışır ─────────────────────────

    [Fact]
    public async Task SkillController_Index_WithValidToken_CallsApiAndReturnsView()
    {
        // Arrange
        var mock = new Mock<ISkillApiClient>(MockBehavior.Loose);
        mock.Setup(c => c.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SkillAdminDto>)new List<SkillAdminDto>
            {
                new() { Id = 1, Name = "C#", Proficiency = 90, IsActive = true }
            }.AsReadOnly(), 1));
        mock.Setup(c => c.GetAdminCountsAsync(It.IsAny<AdminListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusCountsModel { Total = 1, Active = 1 });

        var sut = new SkillController(mock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext("valid-jwt-token")
        };

        // Act
        var result = await sut.Index(null, null, null, null, null, 1, 10, CancellationToken.None);

        // Assert: ViewResult dönmeli (redirect değil)
        result.Should().BeOfType<ViewResult>();
        mock.Verify(c => c.GetAdminPagedAsync(It.IsAny<AdminListRequest>(), "valid-jwt-token", It.IsAny<CancellationToken>()), Times.Once);
    }
}
