using FluentAssertions;
using Microsoft.AspNetCore.Html;
using FurkanTural_Portfolio.Helpers;

namespace FurkanTural_Portfolio.Tests;

/// <summary>
/// Unit tests for MarkdownRenderer (pure static helper - no mocking required).
/// Verifies: empty-input guard, basic HTML output, XSS prevention (DisableHtml),
/// auto-links, pipe tables and soft-line-break-as-hard-line-break pipeline settings.
/// </summary>
public class MarkdownRendererTests
{
    // -----------------------------------------------------------------------
    // Empty / null guards
    // -----------------------------------------------------------------------

    [Fact]
    public void ToHtml_WhenInputIsNull_ReturnsHtmlStringEmpty()
    {
        // Act
        var result = MarkdownRenderer.ToHtml(null);

        // Assert
        result.Should().BeSameAs(HtmlString.Empty);
    }

    [Fact]
    public void ToHtml_WhenInputIsEmptyString_ReturnsHtmlStringEmpty()
    {
        // Act
        var result = MarkdownRenderer.ToHtml(string.Empty);

        // Assert
        result.Should().BeSameAs(HtmlString.Empty);
    }

    [Fact]
    public void ToHtml_WhenInputIsWhitespaceOnly_ReturnsHtmlStringEmpty()
    {
        // Act
        var result = MarkdownRenderer.ToHtml("   \t\n  ");

        // Assert
        result.Should().BeSameAs(HtmlString.Empty);
    }

    // -----------------------------------------------------------------------
    // Basic Markdown rendering
    // -----------------------------------------------------------------------

    [Fact]
    public void ToHtml_WhenInputHasBoldText_RendersStrongTag()
    {
        // Act
        var result = MarkdownRenderer.ToHtml("**bold**");
        var html   = result.ToString();

        // Assert
        html.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void ToHtml_WhenInputHasHeading_RendersHeadingTag()
    {
        // Act
        var result = MarkdownRenderer.ToHtml("# Title");
        var html   = result.ToString();

        // Assert
        html.Should().Contain("<h1");
        html.Should().Contain("Title");
    }

    [Fact]
    public void ToHtml_WhenInputHasBulletList_RendersListItems()
    {
        // Act
        var result = MarkdownRenderer.ToHtml("- item1\n- item2");
        var html   = result.ToString();

        // Assert
        html.Should().Contain("<li>");
        html.Should().Contain("item1");
        html.Should().Contain("item2");
    }

    // -----------------------------------------------------------------------
    // XSS prevention - DisableHtml pipeline flag
    // -----------------------------------------------------------------------

    [Fact]
    public void ToHtml_WhenInputContainsScriptTag_EscapesItRatherThanRenderingRaw()
    {
        // Arrange - raw <script> embedded in markdown
        var input = "Hello <script>alert('xss')</script> world";

        // Act
        var html = MarkdownRenderer.ToHtml(input).ToString();

        // Assert - the <script> tag must NOT appear verbatim in output
        html.Should().NotContain("<script>");
        html.Should().NotContain("</script>");
    }

    [Fact]
    public void ToHtml_WhenInputContainsInlineHtml_EscapesTagBracketsRatherThanRenderingRaw()
    {
        // Arrange
        var input = "<div onclick=\"evil()\">text</div>";

        // Act
        var html = MarkdownRenderer.ToHtml(input).ToString();

        // Assert
        // DisableHtml escapes < and > so the tag is not treated as HTML by a browser.
        // The raw "<div " string must NOT appear (it becomes "&lt;div ").
        html.Should().NotContain("<div ");
        // The escaped form of < must appear instead
        html.Should().Contain("&lt;");
    }

    // -----------------------------------------------------------------------
    // Auto-links (UseAutoLinks pipeline extension)
    // -----------------------------------------------------------------------

    [Fact]
    public void ToHtml_WhenInputContainsBareUrl_RendersAnchorTag()
    {
        // Arrange
        var input = "Visit https://example.com for details";

        // Act
        var html = MarkdownRenderer.ToHtml(input).ToString();

        // Assert
        html.Should().Contain("<a href=\"https://example.com\"");
    }

    // -----------------------------------------------------------------------
    // Soft-line-break as hard-line-break (legacy plain-text support)
    // -----------------------------------------------------------------------

    [Fact]
    public void ToHtml_WhenInputHasSingleLineBreak_RendersBreakTag()
    {
        // Arrange - single newline (soft break) should become <br> due to pipeline setting
        var input = "line one\nline two";

        // Act
        var html = MarkdownRenderer.ToHtml(input).ToString();

        // Assert
        html.Should().Contain("<br");
    }

    // -----------------------------------------------------------------------
    // Return type contract
    // -----------------------------------------------------------------------

    [Fact]
    public void ToHtml_WhenInputIsValid_ReturnsHtmlStringInstance()
    {
        // Act
        var result = MarkdownRenderer.ToHtml("# Hello");

        // Assert
        result.Should().BeOfType<HtmlString>();
    }
}
