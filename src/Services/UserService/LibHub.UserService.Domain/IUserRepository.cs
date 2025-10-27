namespace LibHub.UserService.Domain;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<List<User>> GetAllAsync();
}
