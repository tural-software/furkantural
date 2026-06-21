using FluentAssertions;
using FurkanTural_Blog.Helpers;
using Microsoft.AspNetCore.Html;

namespace FurkanTural_Blog.Tests;

public class MarkdownRendererTests
{
    // ── ToHtml — null / whitespace ────────────────────────────────────────────

    [Fact]
    public void ToHtml_WhenMarkdownIsNull_ReturnsHtmlStringEmpty()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml(null);

        // Assert
        result.Should().BeSameAs(HtmlString.Empty);
    }

    [Fact]
    public void ToHtml_WhenMarkdownIsEmpty_ReturnsHtmlStringEmpty()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml(string.Empty);

        // Assert
        result.Should().BeSameAs(HtmlString.Empty);
    }

    [Fact]
    public void ToHtml_WhenMarkdownIsWhiteSpace_ReturnsHtmlStringEmpty()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml("   ");

        // Assert
        result.Should().BeSameAs(HtmlString.Empty);
    }

    // ── ToHtml — basic markdown rendering ────────────────────────────────────

    [Fact]
    public void ToHtml_WhenBoldSyntax_RendersStrongElement()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml("**bold**").ToString();

        // Assert
        result.Should().Contain("<strong>");
        result.Should().Contain("bold");
    }

    [Fact]
    public void ToHtml_WhenItalicSyntax_RendersEmElement()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml("*italic*").ToString();

        // Assert
        result.Should().Contain("<em>");
        result.Should().Contain("italic");
    }

    [Fact]
    public void ToHtml_WhenH1Syntax_RendersH1Element()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml("# Heading1").ToString();

        // Assert
        result.Should().Contain("<h1");
        result.Should().Contain("Heading1");
    }

    [Fact]
    public void ToHtml_WhenH2Syntax_RendersH2Element()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml("## Heading2").ToString();

        // Assert
        result.Should().Contain("<h2");
        result.Should().Contain("Heading2");
    }

    [Fact]
    public void ToHtml_WhenMarkdownLink_RendersAnchorWithHref()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml("[click](https://example.com)").ToString();

        // Assert
        result.Should().Contain("<a ");
        result.Should().Contain("href=\"https://example.com\"");
        result.Should().Contain("click");
    }

    [Fact]
    public void ToHtml_WhenBareUrl_RendersClickableLink()
    {
        // Arrange — UseAutoLinks is enabled in pipeline
        var result = MarkdownRenderer.ToHtml("https://example.com").ToString();

        // Assert
        result.Should().Contain("<a ");
        result.Should().Contain("https://example.com");
    }

    [Fact]
    public void ToHtml_WhenPlainParagraph_WrapsInPTag()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToHtml("Hello world").ToString();

        // Assert
        result.Should().Contain("<p>");
        result.Should().Contain("Hello world");
    }

    // ── ToHtml — security (XSS) ───────────────────────────────────────────────

    [Fact]
    public void ToHtml_WhenRawScriptTag_EscapesScriptTag()
    {
        // Arrange — DisableHtml is active in pipeline
        var result = MarkdownRenderer.ToHtml("<script>alert('xss')</script>").ToString();

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("</script>");
    }

    [Fact]
    public void ToHtml_WhenRawInlineEventHandler_RendersAsEscapedText()
    {
        // Arrange — DisableHtml encodes the tag as HTML entities;
        // the browser never executes onerror because the markup is never emitted as raw HTML.
        var result = MarkdownRenderer.ToHtml("<img src=x onerror=alert(1)>").ToString();

        // Assert — the opening angle bracket must be encoded (no raw <img tag)
        result.Should().NotContain("<img ");
        result.Should().Contain("&lt;img");
    }

    // ── ToHtml — table support ────────────────────────────────────────────────

    [Fact]
    public void ToHtml_WhenPipeTableSyntax_RendersTableElement()
    {
        // Arrange — UsePipeTables is enabled in pipeline
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        // Act
        var result = MarkdownRenderer.ToHtml(markdown).ToString();

        // Assert
        result.Should().Contain("<table");
        result.Should().Contain("<td");
    }

    // ── ToPlainText ───────────────────────────────────────────────────────────

    [Fact]
    public void ToPlainText_WhenMarkdownIsNull_ReturnsEmpty()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToPlainText(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToPlainText_WhenMarkdownIsEmpty_ReturnsEmpty()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToPlainText(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToPlainText_WhenMarkdownIsWhiteSpace_ReturnsEmpty()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToPlainText("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToPlainText_WhenBoldMarkdown_StripsMarkersReturnsText()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToPlainText("**bold**");

        // Assert
        result.Should().NotContain("**");
        result.Should().Contain("bold");
    }

    [Fact]
    public void ToPlainText_WhenHeadingMarkdown_StripsHashReturnsText()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToPlainText("## Heading");

        // Assert
        result.Should().NotContain("##");
        result.Should().Contain("Heading");
    }

    [Fact]
    public void ToPlainText_WhenPlainText_ReturnsTextUnchanged()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToPlainText("Hello world");

        // Assert
        result.Should().Be("Hello world");
    }

    [Fact]
    public void ToPlainText_WhenLinkMarkdown_ReturnsLinkTextNotUrl()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToPlainText("[click here](https://example.com)");

        // Assert
        result.Should().Contain("click here");
        result.Should().NotContain("](https");
    }

    [Fact]
    public void ToPlainText_ResultIsTrimmed()
    {
        // Arrange & Act
        var result = MarkdownRenderer.ToPlainText("   Hello   ");

        // Assert — ToPlainText calls .Trim()
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
    }
}
