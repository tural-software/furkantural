using FluentAssertions;
using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Tests;

public class BlogPostViewModelTests
{
    // ── PublishedDisplay ──────────────────────────────────────────────────────

    [Fact]
    public void PublishedDisplay_WhenCreatedAtIsDefault_ReturnsEmpty()
    {
        // Arrange
        var vm = new BlogPostViewModel { CreatedAt = default };

        // Act
        var result = vm.PublishedDisplay;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void PublishedDisplay_WhenCreatedAtIsSet_ReturnsTurkishLongDateFormat()
    {
        // Arrange
        var vm = new BlogPostViewModel { CreatedAt = new DateTime(2026, 6, 18) };

        // Act
        var result = vm.PublishedDisplay;

        // Assert
        result.Should().Be("18 Haziran 2026");
    }

    [Fact]
    public void PublishedDisplay_WhenJanuaryDate_ReturnsTurkishMonthName()
    {
        // Arrange
        var vm = new BlogPostViewModel { CreatedAt = new DateTime(2024, 1, 5) };

        // Act
        var result = vm.PublishedDisplay;

        // Assert
        result.Should().Be("5 Ocak 2024");
    }

    // ── PublishedIso ──────────────────────────────────────────────────────────

    [Fact]
    public void PublishedIso_WhenCreatedAtIsDefault_ReturnsEmpty()
    {
        // Arrange
        var vm = new BlogPostViewModel { CreatedAt = default };

        // Act
        var result = vm.PublishedIso;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void PublishedIso_WhenCreatedAtIsSet_ReturnsIso8601DateString()
    {
        // Arrange
        var vm = new BlogPostViewModel { CreatedAt = new DateTime(2026, 6, 18) };

        // Act
        var result = vm.PublishedIso;

        // Assert
        result.Should().Be("2026-06-18");
    }

    // ── ModifiedIso ───────────────────────────────────────────────────────────

    [Fact]
    public void ModifiedIso_WhenUpdatedAtIsNull_FallsBackToPublishedDate()
    {
        // Arrange — hiç düzenlenmemiş yazı: dateModified = datePublished.
        var vm = new BlogPostViewModel { CreatedAt = new DateTime(2026, 6, 18), UpdatedAt = null };

        // Act + Assert
        vm.ModifiedIso.Should().Be("2026-06-18");
        vm.ModifiedIso.Should().Be(vm.PublishedIso);
    }

    [Fact]
    public void ModifiedIso_WhenUpdatedAtIsSet_ReturnsUpdatedDate()
    {
        // Arrange — düzenlenmiş yazı: dateModified gerçek güncelleme tarihini taşır.
        var vm = new BlogPostViewModel { CreatedAt = new DateTime(2026, 6, 18), UpdatedAt = new DateTime(2026, 7, 1) };

        // Act + Assert
        vm.ModifiedIso.Should().Be("2026-07-01");
    }

    [Fact]
    public void ModifiedIso_WhenNoDates_ReturnsEmpty()
    {
        // Arrange
        var vm = new BlogPostViewModel { CreatedAt = default, UpdatedAt = null };

        // Act + Assert
        vm.ModifiedIso.Should().BeEmpty();
    }

    // ── ReadingMinutes ────────────────────────────────────────────────────────

    [Fact]
    public void ReadingMinutes_WhenContentIsNull_ReturnsOne()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = null };

        // Act
        var result = vm.ReadingMinutes;

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void ReadingMinutes_WhenContentIsWhiteSpace_ReturnsOne()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = "   " };

        // Act
        var result = vm.ReadingMinutes;

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void ReadingMinutes_WhenContentIsEmpty_ReturnsOne()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = string.Empty };

        // Act
        var result = vm.ReadingMinutes;

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void ReadingMinutes_WhenContentHasLessThan200Words_ReturnsOne()
    {
        // Arrange — 10 words
        var vm = new BlogPostViewModel { Content = "Bir iki uc dort bes alti yedi sekiz dokuz on" };

        // Act
        var result = vm.ReadingMinutes;

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void ReadingMinutes_WhenContentHasExactly200Words_ReturnsOne()
    {
        // Arrange — exactly 200 words => ceil(200/200) = 1
        var content = string.Join(" ", Enumerable.Repeat("kelime", 200));
        var vm = new BlogPostViewModel { Content = content };

        // Act
        var result = vm.ReadingMinutes;

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void ReadingMinutes_WhenContentHas201Words_ReturnsTwo()
    {
        // Arrange — 201 words => ceil(201/200) = 2
        var content = string.Join(" ", Enumerable.Repeat("kelime", 201));
        var vm = new BlogPostViewModel { Content = content };

        // Act
        var result = vm.ReadingMinutes;

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void ReadingMinutes_WhenContentHas400Words_ReturnsTwo()
    {
        // Arrange — 400 words => ceil(400/200) = 2
        var content = string.Join(" ", Enumerable.Repeat("kelime", 400));
        var vm = new BlogPostViewModel { Content = content };

        // Act
        var result = vm.ReadingMinutes;

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void ReadingMinutes_WhenContentHas1000Words_ReturnsFive()
    {
        // Arrange — 1000 words => ceil(1000/200) = 5
        var content = string.Join(" ", Enumerable.Repeat("kelime", 1000));
        var vm = new BlogPostViewModel { Content = content };

        // Act
        var result = vm.ReadingMinutes;

        // Assert
        result.Should().Be(5);
    }

    // ── Excerpt ───────────────────────────────────────────────────────────────

    [Fact]
    public void Excerpt_WhenContentIsNull_ReturnsEmpty()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = null };

        // Act
        var result = vm.Excerpt;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Excerpt_WhenContentIsWhiteSpace_ReturnsEmpty()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = "   " };

        // Act
        var result = vm.Excerpt;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Excerpt_WhenContentIsUnder160Chars_ReturnsPlainText()
    {
        // Arrange — short plain text (no markdown)
        var vm = new BlogPostViewModel { Content = "Kisa bir blog yazisi." };

        // Act
        var result = vm.Excerpt;

        // Assert
        result.Should().Be("Kisa bir blog yazisi.");
    }

    [Fact]
    public void Excerpt_WhenMarkdownContent_StripsSyntaxInExcerpt()
    {
        // Arrange — markdown headers should be stripped
        var vm = new BlogPostViewModel { Content = "## Baslik\nKalin metin ve normal metin." };

        // Act
        var result = vm.Excerpt;

        // Assert — markdown markers should not appear in excerpt
        result.Should().NotContain("##");
    }

    [Fact]
    public void Excerpt_WhenContentExceeds160Chars_EndsWithEllipsis()
    {
        // Arrange — 25 words of 10 chars = 274 chars total (plain text > 160)
        var content = string.Join(" ", Enumerable.Repeat("uzunkelime", 25));
        var vm = new BlogPostViewModel { Content = content };

        // Act
        var result = vm.Excerpt;

        // Assert
        result.Should().EndWith("…");
    }

    [Fact]
    public void Excerpt_WhenContentExceeds160Chars_LengthIsAtMost161()
    {
        // Arrange — 50 a + space + 50 b + space + 80 c = 182 plain chars
        var content = new string('a', 50) + " " + new string('b', 50) + " " + new string('c', 80);
        var vm = new BlogPostViewModel { Content = content };

        // Act
        var result = vm.Excerpt;

        // Assert — 160 chars max + ellipsis (1 char)
        result.Length.Should().BeLessThanOrEqualTo(161);
    }

    [Fact]
    public void Excerpt_WhenContentIsExactly160PlainChars_DoesNotAddEllipsis()
    {
        // Arrange — plain text exactly 160 chars (no markdown)
        var content = new string('x', 160);
        var vm = new BlogPostViewModel { Content = content };

        // Act
        var result = vm.Excerpt;

        // Assert
        result.Should().NotEndWith("…");
    }

    // ── ContentHtml ───────────────────────────────────────────────────────────

    [Fact]
    public void ContentHtml_WhenContentIsNull_ReturnsEmptyHtmlString()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = null };

        // Act
        var result = vm.ContentHtml;

        // Assert
        result.Should().NotBeNull();
        result.ToString().Should().BeEmpty();
    }

    [Fact]
    public void ContentHtml_WhenContentIsWhiteSpace_ReturnsEmptyHtmlString()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = "   " };

        // Act
        var result = vm.ContentHtml;

        // Assert
        result.ToString().Should().BeEmpty();
    }

    [Fact]
    public void ContentHtml_WhenContentHasBoldMarkdown_RendersStrongTag()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = "**kalin**" };

        // Act
        var result = vm.ContentHtml.ToString();

        // Assert
        result.Should().Contain("<strong>");
        result.Should().Contain("kalin");
    }

    [Fact]
    public void ContentHtml_WhenContentHasHeadingMarkdown_RendersHTag()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = "## Baslik" };

        // Act
        var result = vm.ContentHtml.ToString();

        // Assert
        result.Should().Contain("<h2");
        result.Should().Contain("Baslik");
    }

    [Fact]
    public void ContentHtml_WhenContentHasScriptTag_EscapesIt()
    {
        // Arrange — XSS: raw HTML is disabled in pipeline (DisableHtml)
        var vm = new BlogPostViewModel { Content = "<script>alert(xss)</script>" };

        // Act
        var result = vm.ContentHtml.ToString();

        // Assert — script tag should be escaped, not rendered as-is
        result.Should().NotContain("<script>");
    }

    [Fact]
    public void ContentHtml_WhenContentHasLink_RendersAnchorTag()
    {
        // Arrange
        var vm = new BlogPostViewModel { Content = "[tikla](https://example.com)" };

        // Act
        var result = vm.ContentHtml.ToString();

        // Assert
        result.Should().Contain("<a ");
        result.Should().Contain("https://example.com");
    }
}
