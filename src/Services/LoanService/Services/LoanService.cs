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
        _logger.LogInformation(
            "🚀 [SAGA-START] BorrowBook | UserId: {UserId} | BookId: {BookId}", 
            userId, request.BookId);

        // Check loan limit
        var activeLoansCount = await _loanRepository.CountActiveLoansForUserAsync(userId);
        if (activeLoansCount >= 5)
        {
            _logger.LogWarning(
                "💥 [SAGA-FAILED] User has reached max loan limit | UserId: {UserId} | ActiveLoans: {ActiveLoans}", 
                userId, activeLoansCount);
            throw new InvalidOperationException("Maximum loan limit reached (5 active loans)");
        }

        // Create PENDING loan
        var loan = new Loan(userId, request.BookId);
        await _loanRepository.AddAsync(loan);
        _logger.LogInformation(
            "📝 [SAGA-STEP-1] Loan record created | LoanId: {LoanId} | Status: PENDING", 
            loan.LoanId);

        try
        {
            // Verify book availability
            _logger.LogInformation(
                "🔍 [SAGA-STEP-2] Checking book availability | BookId: {BookId}", 
                request.BookId);
            
            var book = await _catalogService.GetBookAsync(request.BookId);
            
            if (!book.IsAvailable)
            {
                _logger.LogWarning(
                    "💥 [SAGA-STEP-2-FAILED] Book not available | BookId: {BookId}", 
                    request.BookId);
                throw new InvalidOperationException("Book is not available");
            }

            _logger.LogInformation(
                "✅ [SAGA-STEP-2-SUCCESS] Book is available | BookId: {BookId} | AvailableCopies: {AvailableCopies}", 
                request.BookId, book.AvailableCopies);

            // Decrement stock
            _logger.LogInformation(
                "📉 [SAGA-STEP-3] Decrementing book stock | BookId: {BookId}", 
                request.BookId);
            
            await _catalogService.DecrementStockAsync(request.BookId);
            
            _logger.LogInformation(
                "✅ [SAGA-STEP-3-SUCCESS] Stock decremented successfully | BookId: {BookId}", 
                request.BookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "💥 [SAGA-FAILED] Saga failed | BookId: {BookId} | Reason: {Reason}", 
                request.BookId, ex.Message);
            
            // Compensating transaction
            if (loan.Status == "PENDING")
            {
                loan.MarkAsFailed();
                await _loanRepository.UpdateAsync(loan);
                _logger.LogWarning(
                    "🔄 [SAGA-COMPENSATION] Marked loan as FAILED | LoanId: {LoanId}", 
                    loan.LoanId);
            }
            
            throw new InvalidOperationException($"Failed to borrow book: {ex.Message}", ex);
        }

        // Mark loan as CheckedOut
        loan.MarkAsCheckedOut();
        await _loanRepository.UpdateAsync(loan);
        
        _logger.LogInformation(
            "🎉 [SAGA-SUCCESS] Borrow completed | LoanId: {LoanId} | UserId: {UserId} | BookId: {BookId} | DueDate: {DueDate}", 
            loan.LoanId, userId, request.BookId, loan.DueDate);

        return MapToResponse(loan);
    }

    public async Task ReturnBookAsync(int loanId)
    {
        _logger.LogInformation(
            "📚 [RETURN-START] Processing book return | LoanId: {LoanId}", 
            loanId);
        
        var loan = await _loanRepository.GetByIdAsync(loanId);
        if (loan == null)
        {
            _logger.LogWarning(
                "❌ [RETURN-FAILED] Loan not found | LoanId: {LoanId}", 
                loanId);
            throw new InvalidOperationException("Loan not found");
        }

        if (loan.Status == "Returned")
        {
            _logger.LogWarning(
                "❌ [RETURN-FAILED] Loan already returned | LoanId: {LoanId}", 
                loanId);
            throw new InvalidOperationException("Book already returned");
        }
        
        _logger.LogInformation(
            "📝 [RETURN-STEP-1] Marking loan as returned | LoanId: {LoanId}", 
            loanId);
        
        loan.MarkAsReturned();
        await _loanRepository.UpdateAsync(loan);
        
        _logger.LogInformation(
            "✅ [RETURN-STEP-1-SUCCESS] Loan marked as returned | LoanId: {LoanId}", 
            loanId);
        
        try
        {
            _logger.LogInformation(
                "📈 [RETURN-STEP-2] Incrementing book stock | BookId: {BookId}", 
                loan.BookId);
            
            await _catalogService.IncrementStockAsync(loan.BookId);
            
            _logger.LogInformation(
                "✅ [RETURN-STEP-2-SUCCESS] Stock incremented | BookId: {BookId}", 
                loan.BookId);
            
            _logger.LogInformation(
                "🎉 [RETURN-SUCCESS] Return completed successfully | LoanId: {LoanId} | BookId: {BookId}", 
                loanId, loan.BookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "💥 [RETURN-COMPENSATION-START] Stock increment failed, rolling back loan status | LoanId: {LoanId} | BookId: {BookId}", 
                loanId, loan.BookId);
            
            loan.RollbackReturn();
            await _loanRepository.UpdateAsync(loan);
            
            _logger.LogWarning(
                "🔄 [RETURN-COMPENSATION-SUCCESS] Loan reverted to CheckedOut status | LoanId: {LoanId}", 
                loanId);
            
            throw new InvalidOperationException($"Failed to return book: {ex.Message}", ex);
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

