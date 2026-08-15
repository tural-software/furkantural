using FluentAssertions;
using FurkanTural_Chat.Models.Wrappers;

namespace FurkanTural_Chat.Tests.Models;

/// <summary>
/// ApiResult ve ApiResult-T factory metodlarinin davranis testleri.
/// </summary>
public class ApiResultTests
{
    // ---- ApiResult.Fail ----

    [Fact]
    public void ApiResult_Fail_SetsSuccessFalseAndErrorMessage()
    {
        // Act
        var result = ApiResult.Fail("Bir hata olustu.", 400);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().ContainSingle().Which.Should().Be("Bir hata olustu.");
    }

    [Fact]
    public void ApiResult_Fail_DefaultStatusCode_Is500()
    {
        // Act
        var result = ApiResult.Fail("hata");

        // Assert
        result.StatusCode.Should().Be(500);
    }

    [Fact]
    public void ApiResult_Fail_EmptyMessageString_StillAddsToErrors()
    {
        // Act
        var result = ApiResult.Fail(string.Empty);

        // Assert
        result.Errors.Should().ContainSingle().Which.Should().BeEmpty();
    }

    // ---- ApiResult-T.Fail ----

    [Fact]
    public void ApiResultT_Fail_DataIsNullAndSuccessFalse()
    {
        // Act
        var result = ApiResult<string>.Fail("tip hatasi", 422);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Data.Should().BeNull();
        result.Errors.Should().ContainSingle().Which.Should().Be("tip hatasi");
    }

    [Fact]
    public void ApiResultT_Fail_DefaultStatusCode_Is500()
    {
        // Act
        var result = ApiResult<int>.Fail("hata");

        // Assert
        result.StatusCode.Should().Be(500);
        result.Data.Should().Be(0);
    }

    // ---- Varsayilan deger kontrolleri ----

    [Fact]
    public void ApiResult_DefaultCtor_HasSaneDefaults()
    {
        // Act
        var result = new ApiResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().BeEmpty();
        result.StatusCode.Should().Be(200);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ApiResultT_DefaultCtor_DataIsNull()
    {
        // Act
        var result = new ApiResult<string>();

        // Assert
        result.Data.Should().BeNull();
        result.Success.Should().BeFalse();
    }

    // ---- Coklu hata listesi ----

    [Fact]
    public void ApiResult_Fail_CreatesExactlyOneError()
    {
        // Act
        var result = ApiResult.Fail("tek hata");

        // Assert
        result.Errors.Should().HaveCount(1);
    }

    // ---- Success=true el ile set ----

    [Fact]
    public void ApiResultT_ManualSuccess_CanHoldData()
    {
        // Arrange
        var result = new ApiResult<string>
        {
            Success = true,
            StatusCode = 200,
            Data = "veri"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("veri");
        result.Errors.Should().BeEmpty();
    }
}
