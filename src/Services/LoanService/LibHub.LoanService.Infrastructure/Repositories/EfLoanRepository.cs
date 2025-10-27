using LibHub.LoanService.Domain;
using Microsoft.EntityFrameworkCore;

namespace LibHub.LoanService.Infrastructure.Repositories;

public class EfLoanRepository : ILoanRepository
{
    private readonly LoanDbContext _context;

    public EfLoanRepository(LoanDbContext context)
    {
        _context = context;
    }

    public async Task<Loan?> GetByIdAsync(int loanId)
    {
        return await _context.Loans.FindAsync(loanId);
    }

    public async Task<List<Loan>> GetActiveLoansForUserAsync(int userId)
    {
        return await _context.Loans
            .Where(l => l.UserId == userId && l.Status == "CheckedOut")
            .ToListAsync();
    }

    public async Task<List<Loan>> GetAllLoansForUserAsync(int userId)
    {
        return await _context.Loans
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CheckoutDate)
            .ToListAsync();
    }

    public async Task<List<Loan>> GetAllActiveLoansAsync()
    {
        return await _context.Loans
            .Where(l => l.Status == "CheckedOut")
            .OrderBy(l => l.DueDate)
            .ToListAsync();
    }

    public async Task<List<Loan>> GetOverdueLoansAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Loans
            .Where(l => l.Status == "CheckedOut" && l.DueDate < now)
            .OrderBy(l => l.DueDate)
            .ToListAsync();
    }

    public async Task<int> CountActiveLoansForUserAsync(int userId)
    {
        return await _context.Loans
            .CountAsync(l => l.UserId == userId && l.Status == "CheckedOut");
    }

    public async Task AddAsync(Loan loan)
    {
        await _context.Loans.AddAsync(loan);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Loan loan)
    {
        _context.Loans.Update(loan);
        await _context.SaveChangesAsync();
    }
}
