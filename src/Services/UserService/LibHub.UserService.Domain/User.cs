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
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required", nameof(username));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (role != "Customer" && role != "Admin")
            throw new ArgumentException("Invalid role", nameof(role));

        Username = username;
        Email = email.ToLowerInvariant();
        HashedPassword = hashedPassword;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }
}
