using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using FurkanTural_Chat.Models.Auth;

namespace FurkanTural_Chat.Tests.Models;

/// <summary>
/// LoginRequestModel ve RegisterRequestModel DataAnnotations attribute testleri.
/// Validator.TryValidateObject ile model-level validation davranisi dogrulanir.
/// </summary>
public class ValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    // ---- LoginRequestModel ----

    [Fact]
    public void LoginRequestModel_AllValid_NoValidationErrors()
    {
        // Arrange
        var model = new LoginRequestModel { Username = "furkan", Password = "P@ss1234" };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void LoginRequestModel_MissingUsername_HasValidationError()
    {
        // Arrange
        var model = new LoginRequestModel { Username = null, Password = "P@ss" };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().ContainSingle(e =>
            e.MemberNames.Contains(nameof(LoginRequestModel.Username)));
    }

    [Fact]
    public void LoginRequestModel_MissingPassword_HasValidationError()
    {
        // Arrange
        var model = new LoginRequestModel { Username = "furkan", Password = null };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().ContainSingle(e =>
            e.MemberNames.Contains(nameof(LoginRequestModel.Password)));
    }

    [Fact]
    public void LoginRequestModel_TurnstileToken_IsOptional()
    {
        // TurnstileToken attribute yok -- null olunca hata gelmemeli.
        var model = new LoginRequestModel { Username = "u", Password = "p", TurnstileToken = null };

        var errors = Validate(model);

        errors.Should().BeEmpty();
    }

    // ---- RegisterRequestModel ----

    [Fact]
    public void RegisterRequestModel_AllValid_NoValidationErrors()
    {
        // Arrange
        var model = new RegisterRequestModel
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "1234",
            AcceptAgreement = true
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void RegisterRequestModel_MissingUsername_HasValidationError()
    {
        var model = new RegisterRequestModel
        {
            Username = null, Email = "e@e.com", Password = "1234", AcceptAgreement = true
        };
        var errors = Validate(model);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestModel.Username)));
    }

    [Fact]
    public void RegisterRequestModel_UsernameTooShort_HasValidationError()
    {
        // StringLength MinimumLength=3
        var model = new RegisterRequestModel
        {
            Username = "ab", Email = "e@e.com", Password = "1234", AcceptAgreement = true
        };
        var errors = Validate(model);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestModel.Username)));
    }

    [Fact]
    public void RegisterRequestModel_UsernameTooLong_HasValidationError()
    {
        // StringLength MaximumLength=100
        var model = new RegisterRequestModel
        {
            Username = new string('a', 101),
            Email = "e@e.com",
            Password = "1234",
            AcceptAgreement = true
        };
        var errors = Validate(model);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestModel.Username)));
    }

    [Fact]
    public void RegisterRequestModel_InvalidEmail_HasValidationError()
    {
        var model = new RegisterRequestModel
        {
            Username = "validuser", Email = "not-an-email", Password = "1234", AcceptAgreement = true
        };
        var errors = Validate(model);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestModel.Email)));
    }

    [Fact]
    public void RegisterRequestModel_MissingEmail_HasValidationError()
    {
        var model = new RegisterRequestModel
        {
            Username = "validuser", Email = null, Password = "1234", AcceptAgreement = true
        };
        var errors = Validate(model);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestModel.Email)));
    }

    [Fact]
    public void RegisterRequestModel_PasswordTooShort_HasValidationError()
    {
        // MinLength=4
        var model = new RegisterRequestModel
        {
            Username = "validuser", Email = "e@e.com", Password = "abc", AcceptAgreement = true
        };
        var errors = Validate(model);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestModel.Password)));
    }

    [Fact]
    public void RegisterRequestModel_AcceptAgreement_False_HasValidationError()
    {
        // Range(bool, "true", "true") -- false olmamali
        var model = new RegisterRequestModel
        {
            Username = "validuser", Email = "e@e.com", Password = "1234", AcceptAgreement = false
        };
        var errors = Validate(model);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestModel.AcceptAgreement)));
    }

    [Fact]
    public void RegisterRequestModel_DisplayName_IsOptional()
    {
        // DisplayName attribute yok (sadece Display) -- null olsa da gecerli
        var model = new RegisterRequestModel
        {
            Username = "validuser", Email = "e@e.com", Password = "1234",
            AcceptAgreement = true, DisplayName = null
        };
        var errors = Validate(model);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void RegisterRequestModel_MissingPassword_HasValidationError()
    {
        var model = new RegisterRequestModel
        {
            Username = "validuser", Email = "e@e.com", Password = null, AcceptAgreement = true
        };
        var errors = Validate(model);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestModel.Password)));
    }
}
