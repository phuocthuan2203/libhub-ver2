using System.Security.Claims;
using LibHub.LoanService.Application.DTOs;
using LibHub.LoanService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibHub.LoanService.Api.Controllers;

[ApiController]
[Route("api/loans")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly LoanApplicationService _loanService;
    private readonly ILogger<LoansController> _logger;

    public LoansController(LoanApplicationService loanService, ILogger<LoansController> logger)
    {
        _loanService = loanService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> BorrowBook(CreateLoanDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var loan = await _loanService.BorrowBookAsync(userId, dto);
            return CreatedAtAction(nameof(GetLoanById), new { id = loan.LoanId }, loan);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Borrow book failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error borrowing book");
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    [HttpPut("{id}/return")]
    public async Task<IActionResult> ReturnBook(int id)
    {
        try
        {
            await _loanService.ReturnBookAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Return book failed for LoanId={LoanId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error returning book LoanId={LoanId}", id);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserLoans(int userId)
    {
        try
        {
            var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && currentUserId != userId)
            {
                return Forbid();
            }

            var loans = await _loanService.GetUserLoansAsync(userId);
            return Ok(loans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loans for UserId={UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllLoans()
    {
        try
        {
            var loans = await _loanService.GetAllLoansAsync();
            return Ok(loans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all loans");
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoanById(int id)
    {
        try
        {
            var loan = await _loanService.GetLoanByIdAsync(id);
            if (loan == null)
            {
                return NotFound(new { message = "Loan not found" });
            }

            var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && loan.UserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(loan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan LoanId={LoanId}", id);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }
}
