namespace LibHub.LoanService.Domain;

public class Loan
{
    public int LoanId { get; private set; }
    public int UserId { get; private set; }
    public int BookId { get; private set; }
    public DateTime CheckoutDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? ReturnDate { get; private set; }
    public string Status { get; private set; } = string.Empty;

    public bool IsOverdue => Status == "CheckedOut" && DateTime.UtcNow > DueDate;

    private Loan() { }

    public Loan(int userId, int bookId)
    {
        if (userId <= 0) 
            throw new ArgumentException("Invalid user ID", nameof(userId));
        if (bookId <= 0) 
            throw new ArgumentException("Invalid book ID", nameof(bookId));

        UserId = userId;
        BookId = bookId;
        CheckoutDate = DateTime.UtcNow;
        DueDate = CheckoutDate.AddDays(14);
        Status = "PENDING";
    }

    public void MarkAsCheckedOut()
    {
        if (Status != "PENDING")
            throw new InvalidOperationException("Can only mark PENDING loans as checked out");
        
        Status = "CheckedOut";
    }

    public void MarkAsFailed()
    {
        if (Status != "PENDING")
            throw new InvalidOperationException("Can only mark PENDING loans as failed");
        
        Status = "FAILED";
    }

    public void MarkAsReturned()
    {
        if (Status != "CheckedOut")
            throw new InvalidOperationException("Can only return checked out loans");
        
        Status = "Returned";
        ReturnDate = DateTime.UtcNow;
    }
}
