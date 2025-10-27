using LibHub.UserService.Application.DTOs;
using LibHub.UserService.Application.Interfaces;
using LibHub.UserService.Application.Validation;
using LibHub.UserService.Domain;

namespace LibHub.UserService.Application.Services;

public class IdentityApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public IdentityApplicationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<UserDto> RegisterUserAsync(RegisterUserDto dto)
    {
        var (isValid, errorMessage) = PasswordValidator.Validate(dto.Password);
        if (!isValid)
            throw new ArgumentException(errorMessage);

        if (await _userRepository.EmailExistsAsync(dto.Email))
            throw new InvalidOperationException("Email already exists");

        var hashedPassword = _passwordHasher.Hash(dto.Password);

        var user = new User(dto.Username, dto.Email, hashedPassword, "Customer");

        await _userRepository.AddAsync(user);

        return MapToDto(user);
    }

    public async Task<TokenDto> LoginUserAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.VerifyPassword(dto.Password, _passwordHasher))
            throw new UnauthorizedAccessException("Invalid email or password");

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new TokenDto
        {
            AccessToken = token,
            ExpiresIn = 3600
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user != null ? MapToDto(user) : null;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}
