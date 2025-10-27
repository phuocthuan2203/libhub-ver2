using LibHub.UserService.Domain;

namespace LibHub.UserService.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(password))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
