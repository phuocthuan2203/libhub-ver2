using LibHub.UserService.Models.Entities;
using LibHub.UserService.Models.Requests;
using LibHub.UserService.Models.Responses;
using LibHub.UserService.Data;
using LibHub.UserService.Security;

namespace LibHub.UserService.Services;

public class UserService
{
    private readonly UserRepository _repository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _tokenGenerator;

    public UserService(
        UserRepository repository,
        PasswordHasher passwordHasher,
        JwtTokenGenerator tokenGenerator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        var (isValid, errorMessage) = PasswordValidator.Validate(request.Password);
        if (!isValid)
            throw new ArgumentException(errorMessage);

        if (await _repository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already exists");

        var hashedPassword = _passwordHasher.Hash(request.Password);

        var user = new User(request.Username, request.Email, hashedPassword, "Customer");

        await _repository.AddAsync(user);

        return MapToResponse(user);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await _repository.GetByEmailAsync(request.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.VerifyPassword(request.Password, _passwordHasher))
            throw new UnauthorizedAccessException("Invalid email or password");

        var token = _tokenGenerator.GenerateToken(user);

        return new TokenResponse
        {
            AccessToken = token,
            ExpiresIn = 3600
        };
    }

    public async Task<UserResponse?> GetByIdAsync(int userId)
    {
        var user = await _repository.GetByIdAsync(userId);
        return user != null ? MapToResponse(user) : null;
    }

    public async Task<UserResponse?> GetByEmailAsync(string email)
    {
        var user = await _repository.GetByEmailAsync(email);
        return user != null ? MapToResponse(user) : null;
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}

