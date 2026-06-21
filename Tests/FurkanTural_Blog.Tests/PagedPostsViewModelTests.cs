using FluentAssertions;
using FurkanTural_Blog.Models;

namespace FurkanTural_Blog.Tests;

public class PagedPostsViewModelTests
{
    // ── HasPrevious ───────────────────────────────────────────────────────────

    [Fact]
    public void HasPrevious_WhenPageNumberIsOne_ReturnsFalse()
    {
        // Arrange
        var vm = new PagedPostsViewModel { PageNumber = 1, TotalPages = 5 };

        // Act & Assert
        vm.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void HasPrevious_WhenPageNumberIsGreaterThanOne_ReturnsTrue()
    {
        // Arrange
        var vm = new PagedPostsViewModel { PageNumber = 2, TotalPages = 5 };

        // Act & Assert
        vm.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public void HasPrevious_WhenPageNumberIsLastPage_ReturnsTrue()
    {
        // Arrange
        var vm = new PagedPostsViewModel { PageNumber = 5, TotalPages = 5 };

        // Act & Assert
        vm.HasPrevious.Should().BeTrue();
    }

    // ── HasNext ───────────────────────────────────────────────────────────────

    [Fact]
    public void HasNext_WhenPageNumberEqualsTotal_ReturnsFalse()
    {
        // Arrange
        var vm = new PagedPostsViewModel { PageNumber = 5, TotalPages = 5 };

        // Act & Assert
        vm.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasNext_WhenPageNumberIsLessThanTotal_ReturnsTrue()
    {
        // Arrange
        var vm = new PagedPostsViewModel { PageNumber = 1, TotalPages = 5 };

        // Act & Assert
        vm.HasNext.Should().BeTrue();
    }

    [Fact]
    public void HasNext_WhenSinglePageAndOnPageOne_ReturnsFalse()
    {
        // Arrange
        var vm = new PagedPostsViewModel { PageNumber = 1, TotalPages = 1 };

        // Act & Assert
        vm.HasNext.Should().BeFalse();
    }

    // ── HasActiveFilter ───────────────────────────────────────────────────────

    [Fact]
    public void HasActiveFilter_WhenNoFilters_ReturnsFalse()
    {
        // Arrange
        var vm = new PagedPostsViewModel { CategoryId = null, Search = null };

        // Act & Assert
        vm.HasActiveFilter.Should().BeFalse();
    }

    [Fact]
    public void HasActiveFilter_WhenCategoryIdSet_ReturnsTrue()
    {
        // Arrange
        var vm = new PagedPostsViewModel { CategoryId = 3, Search = null };

        // Act & Assert
        vm.HasActiveFilter.Should().BeTrue();
    }

    [Fact]
    public void HasActiveFilter_WhenSearchSet_ReturnsTrue()
    {
        // Arrange
        var vm = new PagedPostsViewModel { CategoryId = null, Search = "dotnet" };

        // Act & Assert
        vm.HasActiveFilter.Should().BeTrue();
    }

    [Fact]
    public void HasActiveFilter_WhenSearchIsWhiteSpaceOnly_ReturnsFalse()
    {
        // Arrange
        var vm = new PagedPostsViewModel { CategoryId = null, Search = "   " };

        // Act & Assert
        vm.HasActiveFilter.Should().BeFalse();
    }

    [Fact]
    public void HasActiveFilter_WhenBothFiltersSet_ReturnsTrue()
    {
        // Arrange
        var vm = new PagedPostsViewModel { CategoryId = 2, Search = "dotnet" };

        // Act & Assert
        vm.HasActiveFilter.Should().BeTrue();
    }

    // ── TotalPages consistency ────────────────────────────────────────────────

    [Fact]
    public void PagedPostsViewModel_WhenConstructedWithValidData_HoldsCorrectPaginationState()
    {
        // Arrange & Act
        var vm = new PagedPostsViewModel
        {
            PageNumber = 3,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = (int)Math.Ceiling(25.0 / 10),
            Items = new List<BlogPostViewModel>().AsReadOnly()
        };

        // Assert
        vm.TotalPages.Should().Be(3);
        vm.HasPrevious.Should().BeTrue();
        vm.HasNext.Should().BeFalse();
    }

    [Fact]
    public void PagedPostsViewModel_WhenEmptyResult_HasNoPreviousOrNext()
    {
        // Arrange & Act
        var vm = new PagedPostsViewModel
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0,
            TotalPages = 0,
            Items = new List<BlogPostViewModel>().AsReadOnly()
        };

        // Assert
        vm.HasPrevious.Should().BeFalse();
        vm.HasNext.Should().BeFalse();
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public void PagedPostsViewModel_DefaultItems_IsEmptyCollection()
    {
        // Arrange & Act
        var vm = new PagedPostsViewModel();

        // Assert
        vm.Items.Should().NotBeNull();
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public void PagedPostsViewModel_DefaultAvailableCategories_IsEmptyCollection()
    {
        // Arrange & Act
        var vm = new PagedPostsViewModel();

        // Assert
        vm.AvailableCategories.Should().NotBeNull();
        vm.AvailableCategories.Should().BeEmpty();
    }
}
