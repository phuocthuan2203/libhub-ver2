using FluentAssertions;
using LibHub.UserService.Application.DTOs;
using LibHub.UserService.Application.Interfaces;
using LibHub.UserService.Application.Services;
using LibHub.UserService.Domain;
using Moq;
using Xunit;

namespace LibHub.UserService.Tests.Application;

public class IdentityApplicationServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenGenerator> _mockTokenGenerator;
    private readonly IdentityApplicationService _service;

    public IdentityApplicationServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTokenGenerator = new Mock<IJwtTokenGenerator>();
        
        _service = new IdentityApplicationService(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockTokenGenerator.Object);
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidData_ShouldCreateUser()
    {
        var dto = new RegisterUserDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "ValidPass123!"
        };

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hashedpassword123456789012345678901234567890123456789012345");

        var result = await _service.RegisterUserAsync(dto);

        result.Should().NotBeNull();
        result.Username.Should().Be(dto.Username);
        result.Email.Should().Be(dto.Email.ToLower());
        result.Role.Should().Be("Customer");

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        var dto = new RegisterUserDto
        {
            Username = "testuser",
            Email = "existing@example.com",
            Password = "ValidPass123!"
        };

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        Func<Task> act = async () => await _service.RegisterUserAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task LoginUserAsync_WithValidCredentials_ShouldReturnToken()
    {
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = "ValidPass123!"
        };

        var user = new User(
            "testuser",
            dto.Email,
            "hashedpassword123456789012345678901234567890123456789012345",
            "Customer");

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _mockTokenGenerator
            .Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns("jwt-token-here");

        var result = await _service.LoginUserAsync(dto);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt-token-here");
        result.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task LoginUserAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPass123!"
        };

        var user = new User(
            "testuser",
            dto.Email,
            "hashedpassword123456789012345678901234567890123456789012345",
            "Customer");

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        Func<Task> act = async () => await _service.LoginUserAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
