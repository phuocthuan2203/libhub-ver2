using FluentAssertions;
using LibHub.UserService.Infrastructure.Security;
using Xunit;

namespace LibHub.UserService.Tests.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher;

    public PasswordHasherTests()
    {
        _hasher = new PasswordHasher();
    }

    [Fact]
    public void Hash_WithValidPassword_ShouldReturnHashedPassword()
    {
        var password = "TestPassword123!";

        var hash = _hasher.Hash(password);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Length.Should().BeGreaterThan(50);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "TestPassword123!";
        var hash = _hasher.Hash(password);

        var result = _hasher.Verify(hash, password);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ShouldReturnFalse()
    {
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _hasher.Hash(password);

        var result = _hasher.Verify(hash, wrongPassword);

        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_DifferentPasswordInstances_ShouldProduceDifferentHashes()
    {
        var password = "TestPassword123!";

        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        hash1.Should().NotBe(hash2);
        _hasher.Verify(hash1, password).Should().BeTrue();
        _hasher.Verify(hash2, password).Should().BeTrue();
    }
}
