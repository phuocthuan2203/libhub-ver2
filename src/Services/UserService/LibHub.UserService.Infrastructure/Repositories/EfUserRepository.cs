using Microsoft.EntityFrameworkCore;
using LibHub.UserService.Domain;

namespace LibHub.UserService.Infrastructure.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    public EfUserRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email.ToLower());
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }
}
