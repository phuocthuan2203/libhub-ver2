using FluentAssertions;
using LibHub.UserService.Application.Validation;
using Xunit;

namespace LibHub.UserService.Tests.Application;

public class PasswordValidatorTests
{
    [Fact]
    public void Validate_WithValidPassword_ShouldReturnTrue()
    {
        var password = "ValidPass123!";

        var (isValid, errorMessage) = PasswordValidator.Validate(password);

        isValid.Should().BeTrue();
        errorMessage.Should().BeEmpty();
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("Short1!")]
    public void Validate_WithShortPassword_ShouldReturnFalse(string password)
    {
        var (isValid, errorMessage) = PasswordValidator.Validate(password);

        isValid.Should().BeFalse();
        errorMessage.Should().Contain("8 characters");
    }

    [Fact]
    public void Validate_WithoutUppercase_ShouldReturnFalse()
    {
        var password = "validpass123!";

        var (isValid, errorMessage) = PasswordValidator.Validate(password);

        isValid.Should().BeFalse();
        errorMessage.Should().Contain("uppercase");
    }

    [Fact]
    public void Validate_WithoutLowercase_ShouldReturnFalse()
    {
        var password = "VALIDPASS123!";

        var (isValid, errorMessage) = PasswordValidator.Validate(password);

        isValid.Should().BeFalse();
        errorMessage.Should().Contain("lowercase");
    }

    [Fact]
    public void Validate_WithoutDigit_ShouldReturnFalse()
    {
        var password = "ValidPass!";

        var (isValid, errorMessage) = PasswordValidator.Validate(password);

        isValid.Should().BeFalse();
        errorMessage.Should().Contain("digit");
    }

    [Fact]
    public void Validate_WithoutSpecialCharacter_ShouldReturnFalse()
    {
        var password = "ValidPass123";

        var (isValid, errorMessage) = PasswordValidator.Validate(password);

        isValid.Should().BeFalse();
        errorMessage.Should().Contain("special character");
    }
}
