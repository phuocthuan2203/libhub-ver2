using FluentAssertions;
using LibHub.UserService.Domain;
using Xunit;

namespace LibHub.UserService.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateUser()
    {
        var username = "testuser";
        var email = "TEST@EXAMPLE.COM";
        var hashedPassword = "hashedpassword123456789012345678901234567890123456789012345";
        var role = "Customer";

        var user = new User(username, email, hashedPassword, role);

        user.Username.Should().Be(username);
        user.Email.Should().Be("test@example.com");
        user.HashedPassword.Should().Be(hashedPassword);
        user.Role.Should().Be(role);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_WithInvalidUsername_ShouldThrowArgumentException(string invalidUsername)
    {
        Action act = () => new User(
            invalidUsername, 
            "test@example.com", 
            "hashedpassword123456789012345678901234567890123456789012345", 
            "Customer");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Username*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("noemail")]
    public void Constructor_WithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        Action act = () => new User(
            "testuser", 
            invalidEmail, 
            "hashedpassword123456789012345678901234567890123456789012345", 
            "Customer");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email*");
    }

    [Theory]
    [InlineData("SuperUser")]
    [InlineData("Guest")]
    [InlineData("")]
    public void Constructor_WithInvalidRole_ShouldThrowArgumentException(string invalidRole)
    {
        Action act = () => new User(
            "testuser", 
            "test@example.com", 
            "hashedpassword123456789012345678901234567890123456789012345", 
            invalidRole);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Role*");
    }

    [Fact]
    public void IsCustomer_WithCustomerRole_ShouldReturnTrue()
    {
        var user = new User(
            "testuser", 
            "test@example.com", 
            "hashedpassword123456789012345678901234567890123456789012345", 
            "Customer");

        user.IsCustomer().Should().BeTrue();
        user.IsAdmin().Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_WithAdminRole_ShouldReturnTrue()
    {
        var user = new User(
            "admin", 
            "admin@example.com", 
            "hashedpassword123456789012345678901234567890123456789012345", 
            "Admin");

        user.IsAdmin().Should().BeTrue();
        user.IsCustomer().Should().BeFalse();
    }
}
