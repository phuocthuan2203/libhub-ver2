namespace LibHub.UserService.Application.Validation;

public static class PasswordValidator
{
    public static (bool IsValid, string ErrorMessage) Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password is required");

        if (password.Length < 8)
            return (false, "Password must be at least 8 characters long");

        if (!password.Any(char.IsUpper))
            return (false, "Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            return (false, "Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            return (false, "Password must contain at least one digit");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            return (false, "Password must contain at least one special character");

        return (true, string.Empty);
    }
}
