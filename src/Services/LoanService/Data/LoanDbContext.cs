using Microsoft.EntityFrameworkCore;
using LibHub.LoanService.Models.Entities;

namespace LibHub.LoanService.Data;

public class LoanDbContext : DbContext
{
    public DbSet<Loan> Loans { get; set; }

    public LoanDbContext(DbContextOptions<LoanDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.LoanId);
            
            entity.Property(e => e.UserId)
                .IsRequired();
            
            entity.Property(e => e.BookId)
                .IsRequired();
            
            entity.Property(e => e.CheckoutDate)
                .IsRequired();
            
            entity.Property(e => e.DueDate)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BookId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
        });
    }
}

