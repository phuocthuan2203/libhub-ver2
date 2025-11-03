namespace LibHub.LoanService.Models.Responses;

public class LoanResponse
{
    public int LoanId { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckoutDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
}

