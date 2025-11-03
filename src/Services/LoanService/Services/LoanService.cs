using LibHub.LoanService.Models.Entities;
using LibHub.LoanService.Models.Requests;
using LibHub.LoanService.Models.Responses;
using LibHub.LoanService.Data;
using LibHub.LoanService.Clients;

namespace LibHub.LoanService.Services;

public class LoanService
{
    private readonly LoanRepository _loanRepository;
    private readonly ICatalogServiceClient _catalogService;
    private readonly ILogger<LoanService> _logger;

    public LoanService(
        LoanRepository loanRepository,
        ICatalogServiceClient catalogService,
        ILogger<LoanService> logger)
    {
        _loanRepository = loanRepository;
        _catalogService = catalogService;
        _logger = logger;
    }

    public async Task<LoanResponse> BorrowBookAsync(int userId, BorrowBookRequest request)
    {
        _logger.LogInformation("Starting Saga: BorrowBook for UserId={UserId}, BookId={BookId}", userId, request.BookId);

        var activeLoansCount = await _loanRepository.CountActiveLoansForUserAsync(userId);
        if (activeLoansCount >= 5)
        {
            _logger.LogWarning("Saga aborted: User {UserId} has reached max loan limit", userId);
            throw new InvalidOperationException("Maximum loan limit reached (5 active loans)");
        }

        var loan = new Loan(userId, request.BookId);
        await _loanRepository.AddAsync(loan);
        _logger.LogInformation("Saga Step 2: Created PENDING loan {LoanId}", loan.LoanId);

        try
        {
            _logger.LogInformation("Saga Step 3: Verifying book availability for BookId={BookId}", request.BookId);
            var book = await _catalogService.GetBookAsync(request.BookId);
            
            if (!book.IsAvailable)
            {
                _logger.LogWarning("Saga Step 3 failed: Book {BookId} is not available", request.BookId);
                throw new InvalidOperationException("Book is not available");
            }

            _logger.LogInformation("Saga Step 4: Decrementing stock for BookId={BookId}", request.BookId);
            await _catalogService.DecrementStockAsync(request.BookId);
            _logger.LogInformation("Saga Step 4: Stock decremented successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga failed for BookId={BookId}, executing compensating transaction", request.BookId);
            
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

        return MapToResponse(loan);
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

    public async Task<List<LoanResponse>> GetUserLoansAsync(int userId)
    {
        var loans = await _loanRepository.GetAllLoansForUserAsync(userId);
        return loans.Select(MapToResponse).ToList();
    }

    public async Task<LoanResponse?> GetLoanByIdAsync(int loanId)
    {
        var loan = await _loanRepository.GetByIdAsync(loanId);
        return loan == null ? null : MapToResponse(loan);
    }

    public async Task<List<LoanResponse>> GetAllLoansAsync()
    {
        var loans = await _loanRepository.GetAllActiveLoansAsync();
        return loans.Select(MapToResponse).ToList();
    }

    private LoanResponse MapToResponse(Loan loan) => new LoanResponse
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

