using LibHub.LoanService.Application.DTOs;
using LibHub.LoanService.Application.Interfaces;
using LibHub.LoanService.Domain;
using Microsoft.Extensions.Logging;

namespace LibHub.LoanService.Application.Services;

public class LoanApplicationService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICatalogServiceClient _catalogService;
    private readonly ILogger<LoanApplicationService> _logger;

    public LoanApplicationService(
        ILoanRepository loanRepository,
        ICatalogServiceClient catalogService,
        ILogger<LoanApplicationService> logger)
    {
        _loanRepository = loanRepository;
        _catalogService = catalogService;
        _logger = logger;
    }

    public async Task<LoanDto> BorrowBookAsync(int userId, CreateLoanDto dto)
    {
        _logger.LogInformation("Starting Saga: BorrowBook for UserId={UserId}, BookId={BookId}", userId, dto.BookId);

        var activeLoansCount = await _loanRepository.CountActiveLoansForUserAsync(userId);
        if (activeLoansCount >= 5)
        {
            _logger.LogWarning("Saga aborted: User {UserId} has reached max loan limit", userId);
            throw new InvalidOperationException("Maximum loan limit reached (5 active loans)");
        }

        var loan = new Loan(userId, dto.BookId);
        await _loanRepository.AddAsync(loan);
        _logger.LogInformation("Saga Step 2: Created PENDING loan {LoanId}", loan.LoanId);

        try
        {
            _logger.LogInformation("Saga Step 3: Verifying book availability for BookId={BookId}", dto.BookId);
            var book = await _catalogService.GetBookAsync(dto.BookId);
            
            if (!book.IsAvailable)
            {
                _logger.LogWarning("Saga Step 3 failed: Book {BookId} is not available", dto.BookId);
                throw new InvalidOperationException("Book is not available");
            }

            _logger.LogInformation("Saga Step 4: Decrementing stock for BookId={BookId}", dto.BookId);
            await _catalogService.DecrementStockAsync(dto.BookId);
            _logger.LogInformation("Saga Step 4: Stock decremented successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga failed for BookId={BookId}, executing compensating transaction", dto.BookId);
            
            if (loan.Status == "PENDING")
            {
                loan.MarkAsFailed();
                await _loanRepository.UpdateAsync(loan);
                _logger.LogInformation("Saga compensating transaction: Marked loan {LoanId} as FAILED", loan.LoanId);
            }
            
            throw new InvalidOperationException($"Failed to borrow book: {ex.Message}", ex);
        }

        loan.MarkAsCheckedOut();
        await _loanRepository.UpdateAsync(loan);
        _logger.LogInformation("Saga Step 5: Loan {LoanId} marked as CheckedOut - Saga completed successfully", loan.LoanId);

        return MapToDto(loan);
    }

    public async Task ReturnBookAsync(int loanId)
    {
        var loan = await _loanRepository.GetByIdAsync(loanId);
        if (loan == null)
            throw new InvalidOperationException("Loan not found");

        loan.MarkAsReturned();
        await _loanRepository.UpdateAsync(loan);

        try
        {
            await _catalogService.IncrementStockAsync(loan.BookId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to increment stock for book {BookId}", loan.BookId);
        }
    }

    public async Task<List<LoanDto>> GetUserLoansAsync(int userId)
    {
        var loans = await _loanRepository.GetAllLoansForUserAsync(userId);
        return loans.Select(MapToDto).ToList();
    }

    public async Task<LoanDto?> GetLoanByIdAsync(int loanId)
    {
        var loan = await _loanRepository.GetByIdAsync(loanId);
        return loan == null ? null : MapToDto(loan);
    }

    public async Task<List<LoanDto>> GetAllLoansAsync()
    {
        var loans = await _loanRepository.GetAllActiveLoansAsync();
        return loans.Select(MapToDto).ToList();
    }

    private LoanDto MapToDto(Loan loan) => new LoanDto
    {
        LoanId = loan.LoanId,
        UserId = loan.UserId,
        BookId = loan.BookId,
        Status = loan.Status,
        CheckoutDate = loan.CheckoutDate,
        DueDate = loan.DueDate,
        ReturnDate = loan.ReturnDate,
        IsOverdue = loan.IsOverdue,
        DaysUntilDue = loan.DaysUntilDue()
    };
}
