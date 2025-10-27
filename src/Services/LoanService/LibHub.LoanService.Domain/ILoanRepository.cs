namespace LibHub.LoanService.Domain;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(int loanId);
    Task<List<Loan>> GetActiveLoansForUserAsync(int userId);
    Task<List<Loan>> GetAllLoansForUserAsync(int userId);
    Task<List<Loan>> GetAllActiveLoansAsync();
    Task<List<Loan>> GetOverdueLoansAsync();
    Task<int> CountActiveLoansForUserAsync(int userId);
    Task AddAsync(Loan loan);
    Task UpdateAsync(Loan loan);
}
