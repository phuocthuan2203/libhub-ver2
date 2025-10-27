using LibHub.UserService.Domain;

namespace LibHub.UserService.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
