using FluentAssertions;
using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Tests;

public class CategoryViewModelTests
{
    // ── DisplayColor ──────────────────────────────────────────────────────────

    [Fact]
    public void DisplayColor_WhenColorIsNull_ReturnsCssAccentVar()
    {
        // Arrange
        var vm = new CategoryViewModel { Color = null };

        // Act
        var result = vm.DisplayColor;

        // Assert
        result.Should().Be("var(--accent)");
    }

    [Fact]
    public void DisplayColor_WhenColorIsEmpty_ReturnsCssAccentVar()
    {
        // Arrange
        var vm = new CategoryViewModel { Color = string.Empty };

        // Act
        var result = vm.DisplayColor;

        // Assert
        result.Should().Be("var(--accent)");
    }

    [Fact]
    public void DisplayColor_WhenColorIsWhiteSpace_ReturnsCssAccentVar()
    {
        // Arrange
        var vm = new CategoryViewModel { Color = "   " };

        // Act
        var result = vm.DisplayColor;

        // Assert
        result.Should().Be("var(--accent)");
    }

    [Fact]
    public void DisplayColor_WhenColorIsSet_ReturnsColorValue()
    {
        // Arrange
        var vm = new CategoryViewModel { Color = "#ff6600" };

        // Act
        var result = vm.DisplayColor;

        // Assert
        result.Should().Be("#ff6600");
    }

    [Fact]
    public void DisplayColor_WhenColorIsHslValue_ReturnsHslValue()
    {
        // Arrange
        var vm = new CategoryViewModel { Color = "hsl(200, 80%, 50%)" };

        // Act
        var result = vm.DisplayColor;

        // Assert
        result.Should().Be("hsl(200, 80%, 50%)");
    }

    [Fact]
    public void DisplayColor_WhenColorIsNamedColor_ReturnsNamedColor()
    {
        // Arrange
        var vm = new CategoryViewModel { Color = "blue" };

        // Act
        var result = vm.DisplayColor;

        // Assert
        result.Should().Be("blue");
    }
}
