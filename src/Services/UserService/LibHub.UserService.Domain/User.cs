namespace LibHub.UserService.Domain;

public class User
{
    public int UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string HashedPassword { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private User() { }

    public User(string username, string email, string hashedPassword, string role)
    {
        ValidateUsername(username);
        ValidateEmail(email);
        ValidateRole(role);
        ValidateHashedPassword(hashedPassword);

        Username = username;
        Email = email.ToLowerInvariant();
        HashedPassword = hashedPassword;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public bool VerifyPassword(string plainPassword, IPasswordHasher passwordHasher)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            return false;

        return passwordHasher.Verify(HashedPassword, plainPassword);
    }

    public void UpdateProfile(string username, string email)
    {
        ValidateUsername(username);
        ValidateEmail(email);

        Username = username;
        Email = email.ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required", nameof(username));

        if (username.Length < 2)
            throw new ArgumentException("Username must be at least 2 characters", nameof(username));

        if (username.Length > 100)
            throw new ArgumentException("Username cannot exceed 100 characters", nameof(username));
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (!email.Contains('@') || !email.Contains('.'))
            throw new ArgumentException("Email format is invalid", nameof(email));

        if (email.Length > 255)
            throw new ArgumentException("Email cannot exceed 255 characters", nameof(email));
    }

    private static void ValidateRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required", nameof(role));

        if (role != "Customer" && role != "Admin")
            throw new ArgumentException("Role must be either 'Customer' or 'Admin'", nameof(role));
    }

    private static void ValidateHashedPassword(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Hashed password is required", nameof(hashedPassword));

        if (hashedPassword.Length < 50)
            throw new ArgumentException("Invalid hashed password format", nameof(hashedPassword));
    }

    public bool IsAdmin() => Role == "Admin";
    public bool IsCustomer() => Role == "Customer";
}
