using FluentAssertions;
using FurkanTural_Chat.Models;

namespace FurkanTural_Chat.Tests.Models;

/// <summary>ErrorViewModel.ShowRequestId hesaplanan property testleri.</summary>
public class ErrorViewModelTests
{
    [Fact]
    public void ShowRequestId_NullRequestId_ReturnsFalse()
    {
        // Arrange
        var vm = new ErrorViewModel { RequestId = null };

        // Assert
        vm.ShowRequestId.Should().BeFalse();
    }

    [Fact]
    public void ShowRequestId_EmptyRequestId_ReturnsFalse()
    {
        // Arrange
        var vm = new ErrorViewModel { RequestId = string.Empty };

        // Assert
        vm.ShowRequestId.Should().BeFalse();
    }

    [Fact]
    public void ShowRequestId_ValidRequestId_ReturnsTrue()
    {
        // Arrange
        var vm = new ErrorViewModel { RequestId = "0HN7L2K4P1C3M:00000001" };

        // Assert
        vm.ShowRequestId.Should().BeTrue();
    }

    [Fact]
    public void ShowRequestId_WhitespaceRequestId_ReturnsTrue()
    {
        // string.IsNullOrEmpty(" ") == false, dolayisiyla true doner
        var vm = new ErrorViewModel { RequestId = "   " };
        vm.ShowRequestId.Should().BeTrue();
    }
}
